@echo off

REM Run Payment Service with Dapr sidecar

set APP_ID=payment-service
set APP_PORT=8004
set DAPR_HTTP_PORT=3504
set DAPR_GRPC_PORT=50004
set COMPONENTS_PATH=./.dapr/components
set CONFIG_PATH=./.dapr/config.yaml

echo Starting Payment Service with Dapr...

dapr run ^
  --app-id %APP_ID% ^
  --app-port %APP_PORT% ^
  --dapr-http-port %DAPR_HTTP_PORT% ^
  --dapr-grpc-port %DAPR_GRPC_PORT% ^
  --components-path %COMPONENTS_PATH% ^
  --config %CONFIG_PATH% ^
  --log-level info ^
  -- dotnet run --project PaymentService/PaymentService.csproj
