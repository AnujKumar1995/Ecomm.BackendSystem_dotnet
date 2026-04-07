# Ecommerce Microservices - High Level Design Document

## 1. Identified Microservices

### 1.1 API Gateway Service (Port: 5000)
**Purpose:** Single entry point for all client requests. Routes requests to appropriate downstream microservices.
**Reasoning:** Centralizes cross-cutting concerns (authentication, rate limiting, logging) and provides a unified API surface. Uses Ocelot as the API Gateway library.

### 1.2 Product Service (Port: 5001)
**Purpose:** Manages product inventory — CRUD operations for products (add/remove/list).
**Reasoning:** Core bounded context dealing with the product catalog. Implements CQRS pattern — separating read (queries) and write (commands) models for scalability. Admin operations go through commands; user browsing goes through queries.

### 1.3 Product Detail Service (Port: 5002)
**Purpose:** Manages product details such as size, price, design/color information.
**Reasoning:** Separated from Product Service following the Single Responsibility Principle. Product details (pricing, sizing) change at different rates and for different business reasons than the core product catalog. This allows independent scaling — price lookups are much more frequent than catalog changes.

### 1.4 Cart Service (Port: 5003)
**Purpose:** Manages shopping cart — add items, remove items, view cart, clear cart.
**Reasoning:** Cart operations are user-session-centric and have different scaling needs (high write frequency, short-lived data). Keeping it separate allows independent scaling during peak shopping times.

### 1.5 Order Orchestrator Service (Port: 5004)
**Purpose:** Orchestrates the checkout workflow — validates cart, checks product availability, creates order, triggers notifications.
**Reasoning:** Implements the **Orchestration Pattern** (Saga Orchestrator). The checkout process spans multiple services (Cart, Product, ProductDetail, Notification). Instead of choreography (event-driven coupling), a central orchestrator coordinates the multi-step transaction, making the workflow explicit, debuggable, and easier to manage compensating transactions.

### 1.6 Notification Service (Port: 5005)
**Purpose:** Receives events and logs notifications to console (extensible to email/SMS/push).
**Reasoning:** Decoupled from business services via asynchronous messaging (RabbitMQ). Services publish events; Notification Service subscribes and processes them independently. This ensures notification failures don't block business operations.

---

## 2. Architecture Patterns

### 2.1 CQRS (Command Query Responsibility Segregation)
Applied in **Product Service** and **Product Detail Service**:
- **Commands:** CreateProduct, DeleteProduct, AddProductDetail, RemoveProductDetail
- **Queries:** GetAllProducts, GetProductById, GetProductDetails
- Uses MediatR library for dispatching commands and queries
- Separate command and query handlers with distinct models

### 2.2 Orchestration Pattern (Saga)
Applied in **Order Orchestrator Service**:
- Coordinates the checkout flow: Validate Cart → Check Stock → Create Order → Send Notification
- Centralized workflow logic with explicit state management
- Supports compensating transactions for failure scenarios

### 2.3 Communication Patterns
| Pattern | Usage | Reasoning |
|---------|-------|-----------|
| **Synchronous (HTTP REST)** | Product Service ↔ Product Detail Service, API Gateway → All Services | Request-response needed for real-time data (prices, stock) |
| **Asynchronous (RabbitMQ)** | Order Orchestrator → Notification Service | Fire-and-forget notifications; decouples sender from receiver |

---

## 3. Cross-Cutting Concerns

### 3.1 Logging & Tracing
- **Serilog** for structured logging across all services
- **Correlation ID** propagated via HTTP headers for distributed tracing
- Each request gets a unique trace ID at the API Gateway level

### 3.2 Exception Handling
- Global exception handling middleware in each service
- Standardized error response format (ProblemDetails RFC 7807)
- Circuit breaker pattern via Polly for inter-service HTTP calls

### 3.3 Scalability
| Strategy | Reasoning |
|----------|-----------|
| **Horizontal Scaling** | Each microservice can be independently scaled. Product queries can have 5 replicas while admin commands need only 1. |
| **CQRS Read/Write Split** | Read-heavy workloads (browsing products) scaled independently from write operations (admin updates). |
| **Async Messaging** | RabbitMQ decouples producers from consumers. Notification service can process at its own pace. |
| **Containerization** | Docker enables consistent deployment and easy horizontal scaling via orchestrators (Kubernetes). |
| **API Gateway** | Can implement rate limiting, caching, and load balancing at the edge. |

### 3.4 Security
- JWT-based authentication at the API Gateway
- Role-based authorization (Admin vs User)
- Input validation at each service boundary
- HTTPS enforcement in production

---

## 4. Database Decisions (Discussion)

While this implementation uses in-memory data structures, here are the recommended database choices for production:

| Service | Recommended DB | Reasoning |
|---------|---------------|-----------|
| Product Service | **PostgreSQL** | Relational data with complex queries (filtering, pagination). ACID compliance for inventory. |
| Product Detail Service | **PostgreSQL / MongoDB** | Semi-structured data (varying attributes per product type). MongoDB if schema flexibility needed. |
| Cart Service | **Redis** | Fast read/write, TTL support for cart expiry, session-like data. |
| Order Service | **PostgreSQL** | Transactional integrity for orders. Audit trail requirements. |
| Notification Service | **MongoDB** | Log-style writes, flexible schema for different notification types. |

Each service owns its data (Database per Service pattern) — no shared databases.

---

## 5. High Level Architecture Diagram

```
                    ┌─────────────────────┐
                    │     API Gateway      │
                    │    (Ocelot :5000)    │
                    │  JWT Auth + Routing  │
                    └─────────┬───────────┘
                              │
            ┌─────────────────┼─────────────────┐
            │                 │                 │
    ┌───────▼──────┐  ┌──────▼───────┐  ┌──────▼──────┐
    │   Product    │  │    Cart      │  │   Order     │
    │   Service    │  │   Service    │  │ Orchestrator│
    │  (CQRS)      │  │  (:5003)    │  │  (:5004)    │
    │  (:5001)     │  └──────────────┘  └──────┬──────┘
    └───────┬──────┘                           │
            │ HTTP (sync)                      │ RabbitMQ (async)
    ┌───────▼──────────┐               ┌──────▼──────────┐
    │  Product Detail  │               │  Notification   │
    │    Service       │               │    Service      │
    │   (:5002)        │               │   (:5005)       │
    └──────────────────┘               └─────────────────┘

    ┌──────────────────────────────────────────────────────┐
    │                    RabbitMQ                           │
    │              (Message Broker :5672)                   │
    └──────────────────────────────────────────────────────┘

    ┌──────────────────────────────────────────────────────┐
    │                     Consul                            │
    │             (Service Discovery :8500)                 │
    └──────────────────────────────────────────────────────┘
```

---

## 6. CI/CD Pipeline

See [CI_CD_DIAGRAM.md](CI_CD_DIAGRAM.md) for the full CI/CD pipeline diagram.

---

## 7. Assumptions

1. No UI required — all interactions via REST APIs (Postman).
2. In-memory data storage — Lists and Dictionaries used instead of databases.
3. Single-node deployment via Docker Compose (no Kubernetes).
4. JWT tokens are generated by the API Gateway for simplicity (no separate Auth service).
5. RabbitMQ used for async messaging (Notification events).
6. Consul used for service discovery (services register on startup).
7. All services run in Docker containers on a single host.
8. Admin role is identified via JWT claims.
9. No payment integration — checkout creates an order record.
10. Product IDs are GUIDs generated server-side.
