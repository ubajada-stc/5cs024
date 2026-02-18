## Contributing

Thanks for contributing! Please follow the workflow below to keep the codebase clean and easy to maintain.

Prerequisites

Git installed (git --version)

Access to this private GitHub repository

Workflow Overview

main → stable / production-ready code

dev → active development

Feature work is done in separate branches, never directly on dev or main

Step-by-Step Guide
1. Clone the repository
git clone https://github.com/ubajada-stc/5cs024.git
cd 5cs024

2. Switch to the dev branch
git fetch
git checkout dev


Verify:

git branch


You should see * dev.

3. Create a feature branch

Create a new branch for each task or fix.

git checkout -b feature/short-description


Examples:

git checkout -b feature/user-auth
git checkout -b fix/api-timeout

4. Make your changes

Write code

Add or update tests if applicable

Run the project locally to verify changes

Check status anytime:

git status

5. Commit your changes

Use clear, descriptive commit messages.

git add .
git commit -m "Add user authentication flow"

6. Push your branch
git push -u origin feature/short-description

7. Open a Pull Request

On GitHub:

Base branch: dev

Compare branch: your feature branch

Clearly describe what you changed and why

8. Review & merge

Address any review feedback

Once approved, merge into dev

Delete the feature branch after merging

Keeping Your Branch Up to Date

Before starting new work:

git checkout dev
git pull


If your feature branch falls behind:

git checkout feature/short-description
git merge dev

Rules to Follow

❌ Do not commit directly to main or dev

✅ Always use feature branches

✅ One feature or fix per branch

✅ Keep commits small and focused

✅ `main` is protected and stable

✅ All work goes into `dev`
 
✅ Use pull requests for:
  - Documentation
  - Database changes
  - Security notes




