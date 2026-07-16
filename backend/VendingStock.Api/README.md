# VendingStock API

.NET 10 Web API for the vending-stock warehouse system.

Run locally with a ConnectionStrings__MySql environment variable or appsettings.Local.json, then run dotnet run.

Goods synchronization runs at 04:00 every day. Manual trigger: POST /api/v1/sync-task/goods/trigger.

Production uses docker-compose.prod.yml. Replace the MySQL password placeholder only on the server, never in Git.
