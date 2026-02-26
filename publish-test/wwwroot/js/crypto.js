//Crypto service - to put here web crypto api functions to be called from blazor

window.cryptoService = {

    // Generate a 32-byte random token
    generateToken: async function () {
        const bytes = new Uint8Array(32);
        crypto.getRandomValues(bytes);
        return this._toBase64(bytes);
    },

    // SHA-256 hash (returns base64)
    hashToken: async function (tokenBase64) {
        const tokenBytes = this._fromBase64(tokenBase64);
        const hashBuffer = await crypto.subtle.digest("SHA-256", tokenBytes);
        return this._toBase64(new Uint8Array(hashBuffer));
    },

    // Generate AES-256-GCM symmetric key (returns base64 of raw key bytes)
    generateSymmetricKey: async function () {
        const key = await crypto.subtle.generateKey(
            { name: "AES-GCM", length: 256 },
            true,  // extractable
            ["encrypt", "decrypt"]
        );
        const rawKey = await crypto.subtle.exportKey("raw", key);
        return this._toBase64(new Uint8Array(rawKey));
    },

    // Encrypt with AES-256-GCM (returns { iv, ciphertext, authTag } as base64)
    encryptSymmetric: async function (plaintextBase64, keyBase64) {
        const key = await this._importAesKey(keyBase64);
        const iv = crypto.getRandomValues(new Uint8Array(12));
        const plaintext = this._fromBase64(plaintextBase64);

        const encrypted = await crypto.subtle.encrypt(
            { name: "AES-GCM", iv: iv },
            key,
            plaintext
        );

        // Web Crypto API appends the 16-byte auth tag to the ciphertext
        const encryptedArray = new Uint8Array(encrypted);
        const ciphertext = encryptedArray.slice(0, encryptedArray.length - 16);
        const authTag = encryptedArray.slice(encryptedArray.length - 16);

        return {
            iv: this._toBase64(iv),
            ciphertext: this._toBase64(ciphertext),
            authTag: this._toBase64(authTag)
        };
    },

    // Generate RSA-OAEP 4096-bit keypair (returns { publicKey, privateKey } as base64 SPKI/PKCS8)
    generateKeypair: async function () {
        const keypair = await crypto.subtle.generateKey(
            {
                name: "RSA-OAEP",
                modulusLength: 4096,
                publicExponent: new Uint8Array([1, 0, 1]),
                hash: "SHA-256"
            },
            true,  // extractable
            ["encrypt", "decrypt"]
        );

        const publicKeyBuffer = await crypto.subtle.exportKey("spki", keypair.publicKey);
        const privateKeyBuffer = await crypto.subtle.exportKey("pkcs8", keypair.privateKey);

        return {
            publicKey: this._toBase64(new Uint8Array(publicKeyBuffer)),
            privateKey: this._toBase64(new Uint8Array(privateKeyBuffer))
        };
    },

    // Encrypt symmetric key with RSA-OAEP public key (returns base64 ciphertext)
    encryptWithPublicKey: async function (dataBase64, publicKeyBase64) {
        const publicKey = await crypto.subtle.importKey(
            "spki",
            this._fromBase64(publicKeyBase64),
            { name: "RSA-OAEP", hash: "SHA-256" },
            false,
            ["encrypt"]
        );

        const data = this._fromBase64(dataBase64);
        const encrypted = await crypto.subtle.encrypt(
            { name: "RSA-OAEP" },
            publicKey,
            data
        );

        return this._toBase64(new Uint8Array(encrypted));
    },

    // Encrypt private key with AES-256-GCM wrapping key
    encryptPrivateKey: async function (privateKeyBase64, wrappingKeyBase64) {
        return await this.encryptSymmetric(privateKeyBase64, wrappingKeyBase64);
    },

    // SHA-256 fingerprint of a public key (returns base64)
    fingerprintPublicKey: async function (publicKeyBase64) {
        const keyBytes = this._fromBase64(publicKeyBase64);
        const hash = await crypto.subtle.digest("SHA-256", keyBytes);
        return this._toBase64(new Uint8Array(hash));
    },

    // Convert string to base64 (UTF-8)
    stringToBase64: function (str) {
        const encoder = new TextEncoder();
        const bytes = encoder.encode(str);
        return this._toBase64(bytes);
    },

    //File encryption
    encryptFile: async function (fileContentBase64, fileNameBase64, keyBase64) {
        // bundel filename and content as Json payload - ostja xahna cool bil payload, then we encrypt
        const payload = JSON.stringify({
            fileName: fileNameBase64,
            content: fileContentBase64
        });
        const encoder = new TextEncoder();
        const payloadBytes = encoder.encode(payload); // ergajna bil payload hiiiiii - tas shuttle
        const payloadBase64 = this._toBase64(payloadBytes);
        return await this.encryptSymmetric(payloadBase64, keyBase64);

    },

    // ---- Helper functions ----

    _importAesKey: async function (keyBase64) {
        const keyBytes = this._fromBase64(keyBase64);
        return await crypto.subtle.importKey(
            "raw",
            keyBytes,
            { name: "AES-GCM", length: 256 },
            false,
            ["encrypt", "decrypt"]
        );
    },

    _toBase64: function (uint8Array) {
        let binary = "";
        for (let i = 0; i < uint8Array.length; i++) {
            binary += String.fromCharCode(uint8Array[i]);
        }
        return btoa(binary);
    },

    _fromBase64: function (base64) {
        const binary = atob(base64);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) {
            bytes[i] = binary.charCodeAt(i);
        }
        return bytes;
    }
};