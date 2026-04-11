# Ecommerce Microservices - .NET 9 / C#

A backend ecommerce application built with **Microservices Architecture** using **.NET 9**, implementing **CQRS** and **Orchestration** patterns.

## Architecture Overview

```
Client (Postman) → API Gateway (Ocelot :5000)
                        ├── Product Service (CQRS :5001)
                        │       └── Sync HTTP → Product Detail Service (:5002)
                        ├── Cart Service (:5003)
                        ├── Order Orchestrator (:5004) ── Async RabbitMQ ──→ Notification Service (:5005)
                        └── Auth (JWT Token Generation)

Infrastructure: RabbitMQ (:5672) | Consul (:8500)
```

## Microservices

| Service | Port | Description |
|---------|------|-------------|
| API Gateway | 5000 | Ocelot-based routing, JWT token generation |
| Product Service | 5001 | Product catalog CRUD with CQRS pattern |
| Product Detail Service | 5002 | Manages size, price, design, color |
| Cart Service | 5003 | Shopping cart management |
| Order Orchestrator | 5004 | Checkout workflow orchestration (Saga) |
| Notification Service | 5005 | Event-driven console notifications |

## Design Patterns Used

- **CQRS** (Command Query Responsibility Segregation) - via MediatR in Product & ProductDetail services
- **Orchestration Pattern** (Saga) - Order Orchestrator coordinates checkout across services
- **API Gateway Pattern** - Ocelot routes all traffic through single entry point
- **Database per Service** - Each service has its own in-memory data store
- **Event-Driven Architecture** - RabbitMQ for async notification events

## Communication Patterns

| Type | Between | Protocol |
|------|---------|----------|
| Synchronous | Gateway → All Services | HTTP REST |
| Synchronous | Product Service ↔ Product Detail Service | HTTP REST |
| Synchronous | Order Orchestrator → Cart Service | HTTP REST |
| Asynchronous | Order Orchestrator → Notification Service | RabbitMQ |

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (required)
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) (for local development)
- [Postman](https://www.postman.com/downloads/) (for API testing)

## Quick Start

### Using Docker Compose (Recommended)

```bash
# Clone and navigate to the project
cd Ecommerce_Dotnet

# Build and start all services
docker-compose up --build -d

# Verify all services are running
docker-compose ps

# View logs
docker-compose logs -f

# Stop all services
docker-compose down
```

### Local Development (Without Docker)

```bash
# Build the solution
dotnet build

# Run each service in a separate terminal
dotnet run --project src/ProductService
dotnet run --project src/ProductDetailService
dotnet run --project src/CartService
dotnet run --project src/OrderOrchestratorService
dotnet run --project src/NotificationService
dotnet run --project src/ApiGateway
```

> **Note:** For local development without Docker, set `RabbitMQ:Host` to `localhost` and update `ServiceUrls` in appsettings.

## Restarting a Service

### With Docker Compose

```bash
# Restart one service
docker compose restart api-gateway

# Restart any other API by service name
docker compose restart product-service
docker compose restart product-detail_service
docker compose restart cart.service
docker compose restart order-orchestrator-service
docker compose restart notification-service

# Check status after restart
docker compose ps
```

If you changed code and need the container to pick up a new build, rebuild and recreate that service instead of a plain restart:

```bash
docker compose up --build -d api-gateway
```

Replace `api-gateway` with any service name from `docker-compose.yml`.

### Without Docker

Stop the running API with `Ctrl+C`, then start it again from the repo root:

```bash
dotnet run --project src/ApiGateway
```

Examples for other services:

```bash
dotnet run --project src/ProductService
dotnet run --project src/ProductDetailService
dotnet run --project src/CartService
dotnet run --project src/OrderOrchestratorService
dotnet run --project src/NotificationService
```

## Testing the Flow

### Step-by-step with curl:

```bash
# 1. Get Admin JWT Token
curl -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"email":"admin@ecommerce.com","role":"Admin","userId":"11111111-1111-1111-1111-111111111111"}'

# 2. Get User JWT Token
curl -X POST http://localhost:5000/api/auth/token \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","role":"User"}'

# 3. Create a Product (Admin)
curl -X POST http://localhost:5000/api/products \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <ADMIN_TOKEN>" \
  -d '{"name":"Wireless Headphones","category":"Electronics","description":"Premium BT headphones"}'

# 4. Add Product Detail (Admin)
curl -X POST http://localhost:5000/api/productdetails \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <ADMIN_TOKEN>" \
  -d '{"productId":"<PRODUCT_ID>","size":"Large","price":149.99,"design":"Over-ear","color":"Black","stockQuantity":50}'

# 5. View All Products (Public)
curl http://localhost:5000/api/products?page=1&pageSize=10

# 6. Add to Cart (User)
curl -X POST http://localhost:5000/api/cart/<USER_ID>/items \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <USER_TOKEN>" \
  -d '{"productId":"<PRODUCT_ID>","productDetailId":"<DETAIL_ID>","productName":"Wireless Headphones","size":"Large","price":149.99,"quantity":2}'

# 7. View Cart
curl -H "Authorization: Bearer <USER_TOKEN>" http://localhost:5000/api/cart/<USER_ID>

# 8. Checkout (triggers orchestration + async notification)
curl -X POST -H "Authorization: Bearer <USER_TOKEN>" \
  http://localhost:5000/api/orders/checkout/<USER_ID>
```

### Using Postman
Import the collection from `docs/Ecommerce_Microservices.postman_collection.json` into Postman. Run requests in order (1.1 → 1.2 → 2.1 → 3.1 → 4.1 → 5.1).

## Cross-Cutting Concerns

### Logging & Tracing
- **Serilog** structured logging in all services
- **Correlation ID** propagated across services via `X-Correlation-Id` header
- All requests automatically tracked with trace IDs

### Exception Handling
- Global `ExceptionHandlingMiddleware` in every service
- RFC 7807 ProblemDetails format for error responses
- Typed exception handling (404, 400, 401, 500)

### Security
- JWT-based authentication with role-based authorization
- `Admin` role required for product CRUD operations
- `User` or `Admin` role for cart and checkout
- Product listing is public (no auth required)

### Scalability
- Each microservice independently deployable and scalable
- CQRS allows separate scaling of read/write workloads
- RabbitMQ decouples notification processing
- Docker enables horizontal scaling

## Service Discovery

Consul runs at http://localhost:8500 for service registration and discovery. In the Docker setup, services are discovered via Docker networking (DNS-based).

## Project Structure

```
Ecommerce_Dotnet/
├── docker-compose.yml
├── EcommerceMicroservices.sln
├── docs/
│   ├── DESIGN_DOCUMENT.md
│   ├── CI_CD_DIAGRAM.md
│   └── Ecommerce_Microservices.postman_collection.json
└── src/
    ├── Shared/                    # Shared library (models, middleware, auth, messaging)
    │   ├── Models/
    │   ├── Events/
    │   ├── Middleware/
    │   ├── Auth/
    │   └── Messaging/
    ├── ApiGateway/                # Ocelot API Gateway + Auth endpoint
    ├── ProductService/            # CQRS - Commands, Queries, Handlers
    ├── ProductDetailService/      # CQRS - Size/Price/Design management
    ├── CartService/               # CQRS - Cart management
    ├── OrderOrchestratorService/  # Saga Orchestrator - Checkout workflow
    └── NotificationService/       # Event consumer - Console notifications
```

## Assumptions

1. No UI — all interactions via REST APIs (Postman/curl)
2. In-memory data storage using ConcurrentDictionary (no database)
3. JWT tokens generated by the API Gateway (no separate identity service)
4. RabbitMQ for asynchronous messaging (notifications)
5. Single-node Docker Compose deployment
6. Admin is identified via JWT role claim
7. No payment processing — checkout creates order directly
8. Product listing is public, management requires Admin role
9. Cart and checkout require User or Admin authentication
10. Product IDs are server-generated GUIDs

## Endpoints Summary

### Auth (Gateway)
- `POST /api/auth/token` - Generate JWT token

### Products
- `GET /api/products?page=1&pageSize=10` - List products (public)
- `GET /api/products/{id}` - Get product by ID (public)
- `POST /api/products` - Create product (Admin)
- `DELETE /api/products/{id}` - Delete product (Admin)

### Product Details
- `GET /api/productdetails/product/{productId}` - Get details by product (public)
- `GET /api/productdetails/{id}` - Get detail by ID (public)
- `POST /api/productdetails` - Add detail (Admin)
- `DELETE /api/productdetails/{id}` - Remove detail (Admin)

### Cart
- `GET /api/cart/{userId}` - View cart (Auth)
- `POST /api/cart/{userId}/items` - Add to cart (Auth)
- `DELETE /api/cart/{userId}/items/{itemId}` - Remove item (Auth)
- `DELETE /api/cart/{userId}` - Clear cart (Auth)

### Orders
- `POST /api/orders/checkout/{userId}` - Checkout (Auth)
- `GET /api/orders/{orderId}` - Get order (Auth)
- `GET /api/orders/user/{userId}` - Get user orders (Auth)

### Notification
- `GET /api/notifications/health` - Health check
