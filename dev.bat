@echo off
:loop
echo.
echo ============================================
echo Starting payment service...
echo ============================================
echo.

REM Check if port 5002 is in use and kill the process
echo Checking port 5002...
for /f "tokens=5" %%a in ('netstat -ano ^| findstr :5002 ^| findstr LISTENING') do (
    echo Port 5002 is in use by PID %%a, killing process...
    taskkill /PID %%a /F >nul 2>&1
    timeout /t 1 >nul
)

echo Starting service on port 5002...
dotnet run --project PaymentService/PaymentService.csproj

echo.
echo ============================================
echo Service stopped. Press any key to restart or Ctrl+C to exit.
echo ============================================
pause > nul
goto loop
