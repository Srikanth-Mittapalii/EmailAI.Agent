# Run Backend
Write-Host "Starting Backend..." -ForegroundColor Blue
Start-Process dotnet "run" -WorkingDirectory "./EmailAI.Agent"

# Run Frontend
Write-Host "Starting Frontend..." -ForegroundColor Green
Start-Process npm "run dev" -WorkingDirectory "./EmailAI.Agent/EmailAI.UI"

Write-Host "System is launching. Backend: http://localhost:5180 | Frontend: http://localhost:5173" -ForegroundColor Cyan
