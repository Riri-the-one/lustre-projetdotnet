# Publishing from Windows PowerShell

The active branch was verified as `feat/lustre-portfolio-finalization`. No commit, push, PR, merge or deployment was performed: `.git` is read-only under this session's permission profile. Do not attempt to change those protections. All implementation files are left locally for review.

Automatic approval review also rejected removal of the historical `dotnetProj-main/` folder. It remains excluded from the root project. Its 185 files all have root equivalents (149 were byte-identical at the comparison point; 36 differed, including finalization edits). Review before removing it. Its default-admin seeder was neutralized too. The tracked `ProjetDotNet.csproj.user` remains a local-settings cleanup item; `.gitignore` now prevents future untracked user files from being added.

In a **normal Windows terminal with Git write access**, from the repository root:

```powershell
Set-Location 'C:\Users\bouta\Documents\lustre-projetdotnet'
git status --short
git branch --show-current
# The output must be feat/lustre-portfolio-finalization.
# Preserve local IDE settings while stopping Git tracking them:
git rm --cached -- ProjetDotNet.csproj.user
# After reviewing that no historical variation needs keeping, remove the duplicate snapshot:
git rm -r -- dotnetProj-main

# Use a system .NET 10 SDK, or the already downloaded local SDK in this workspace:
$env:PATH = "$PWD\.tools\dotnet;$env:PATH"
$env:DOTNET_ROOT = "$PWD\.tools\dotnet"
$env:DOTNET_CLI_HOME = "$PWD\.tools\home"
$env:NUGET_PACKAGES = "$PWD\.tools\packages"
$env:NUGET_HTTP_CACHE_PATH = "$PWD\.tools\http-cache"
dotnet restore ProjetDotNet.sln
if ($LASTEXITCODE -ne 0) { throw 'Restore failed' }
dotnet build ProjetDotNet.sln --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
dotnet test ProjetDotNet.sln --configuration Release --no-build
if ($LASTEXITCODE -ne 0) { throw 'Tests failed' }
node scripts/audit-dependencies.cjs
if ($LASTEXITCODE -ne 0) { throw 'Dependency audit failed' }
git diff --check
if ($LASTEXITCODE -ne 0) { throw 'Whitespace check failed' }
git status --short
# Review staged source, screenshots and documents. .tools/.artifacts/databases must stay ignored.
git add --all
git diff --cached --stat
git diff --cached --check
git commit -m "Harden commerce workflows and finalize Lustre .NET 10 portfolio"
git push --set-upstream origin feat/lustre-portfolio-finalization

gh pr create --base main --head feat/lustre-portfolio-finalization --title "Harden commerce workflows and finalize Lustre .NET 10 portfolio" --body-file docs/PR_DESCRIPTION.md
gh pr checks --watch
```

If `gh` is unavailable, open the branch comparison on GitHub and paste `docs/PR_DESCRIPTION.md`. If normal-terminal NuGet networking also fails, repair that environment first; do not commit the temporary `.tools/NuGet.Config` transport configuration. Stop any local demo using build outputs before rebuilding on Windows.

Before publishing, review any historically created administrator account and rotate/disable its old public credentials manually. Do not run provisioning or migrations against Ecom. Migration application, production deployment and PR merging are outside these publication commands.
