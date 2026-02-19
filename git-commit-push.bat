@echo off
REM Run this from Command Prompt AFTER closing Cursor/IDE so .git is not locked.
cd /d D:\PMS_Coditium

git add -A
if errorlevel 1 (
  echo Failed to stage. Make sure no other app has the repo open.
  pause
  exit /b 1
)

git status
git commit -m "Update appsettings.json"
if errorlevel 1 (
  echo Commit failed or nothing to commit.
  pause
  exit /b 1
)

git push origin main
if errorlevel 1 (
  echo Push failed. Check your GitHub auth.
  pause
  exit /b 1
)

echo.
echo Done: committed and pushed to https://github.com/coditiumsolutions/PMS.git
pause
