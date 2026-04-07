# Docker & Docker Compose - Step-by-Step Guide

> A complete explanation of how we designed the Dockerfile and docker-compose.yml for our Ecommerce Microservices project.

---

## Table of Contents

1. [What is Docker?](#1-what-is-docker)
2. [What is a Dockerfile?](#2-what-is-a-dockerfile)
3. [Dockerfile Step-by-Step Breakdown](#3-dockerfile-step-by-step-breakdown)
4. [Why Multi-Stage Build?](#4-why-multi-stage-build)
5. [What is .dockerignore?](#5-what-is-dockerignore)
6. [What is Docker Compose?](#6-what-is-docker-compose)
7. [docker-compose.yml Step-by-Step Breakdown](#7-docker-composeyml-step-by-step-breakdown)
8. [How to Run Everything](#8-how-to-run-everything)
9. [Common Docker Commands](#9-common-docker-commands)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. What is Docker?

Docker is a tool that packages your application and all its dependencies into a **container** — a lightweight, portable box that runs the same way on any machine.

**Why use Docker?**
- "Works on my machine" problem is solved — the container runs identically everywhere
- Each microservice runs in its own isolated container
- Easy to start, stop, and scale services independently

---

## 2. What is a Dockerfile?

A Dockerfile is a **recipe** (text file with instructions) that tells Docker how to build an **image** for your application.

```
Dockerfile  →  (docker build)  →  Image  →  (docker run)  →  Container
 (recipe)                        (snapshot)                 (running app)
```

- **Image** = a read-only snapshot of your app + environment
- **Container** = a running instance of that image

---

## 3. Dockerfile Step-by-Step Breakdown

Our project has 6 Dockerfiles (one per microservice). They all follow the same pattern. Here is the ProductService Dockerfile explained line by line:

```dockerfile
# ============================================
# STAGE 1: BASE (Runtime Image)
# ============================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
```

| Line | What it does |
|------|-------------|
| `FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base` | Start from Microsoft's official ASP.NET 9.0 runtime image. This is a lightweight image that can **run** .NET apps but cannot **build** them. We name this stage `base`. |
| `WORKDIR /app` | Set `/app` as the working directory inside the container. All future commands run from here. |

```dockerfile
# ============================================
# STAGE 2: BUILD (SDK Image - has build tools)
# ============================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
```

| Line | What it does |
|------|-------------|
| `FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build` | Start a NEW stage from the full .NET SDK image. This image is larger (~800MB) because it has compilers, NuGet, MSBuild, etc. We name this stage `build`. |
| `WORKDIR /src` | Set `/src` as the working directory for the build stage. |

```dockerfile
# Copy project files first (for caching)
COPY ["src/Shared/Shared.csproj", "src/Shared/"]
COPY ["src/ProductService/ProductService.csproj", "src/ProductService/"]
RUN dotnet restore "src/ProductService/ProductService.csproj"
```

| Line | What it does |
|------|-------------|
| `COPY ["src/Shared/Shared.csproj", ...]` | Copy ONLY the `.csproj` files first (not the source code). This is a **Docker layer caching trick** — if the project files haven't changed, Docker reuses the cached restore step. |
| `COPY ["src/ProductService/ProductService.csproj", ...]` | Copy the service's project file. We need Shared too because ProductService depends on it. |
| `RUN dotnet restore` | Download all NuGet packages defined in the `.csproj`. This is cached separately so it only re-runs when dependencies change. |

```dockerfile
# Now copy everything else and build
COPY . .
WORKDIR "/src/src/ProductService"
RUN dotnet build "ProductService.csproj" -c Release -o /app/build
```

| Line | What it does |
|------|-------------|
| `COPY . .` | Copy ALL remaining source code into the container. |
| `WORKDIR "/src/src/ProductService"` | Navigate into the service's folder. The path is `/src/src/ProductService` because our `context` is the repo root and source lives under `src/`. |
| `RUN dotnet build -c Release -o /app/build` | Compile the project in Release mode. Output goes to `/app/build`. |

```dockerfile
# ============================================
# STAGE 3: PUBLISH (Optimized output)
# ============================================
FROM build AS publish
RUN dotnet publish "ProductService.csproj" -c Release -o /app/publish /p:UseAppHost=false
```

| Line | What it does |
|------|-------------|
| `FROM build AS publish` | Continue from the `build` stage (don't start fresh). |
| `RUN dotnet publish ... /p:UseAppHost=false` | Create the final optimized output. `publish` trims unnecessary files. `/p:UseAppHost=false` means we don't generate a native executable — we'll use `dotnet` CLI to run it instead. |

```dockerfile
# ============================================
# STAGE 4: FINAL (Small runtime image)
# ============================================
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProductService.dll"]
```

| Line | What it does |
|------|-------------|
| `FROM base AS final` | Switch back to the small `aspnet:9.0` runtime image (~200MB instead of ~800MB). |
| `COPY --from=publish /app/publish .` | Copy ONLY the published output from stage 3 into this clean image. The SDK, source code, and build tools are NOT included. |
| `ENTRYPOINT ["dotnet", "ProductService.dll"]` | Define the command that runs when the container starts. |

---

## 4. Why Multi-Stage Build?

```
Stage 1 (base)     →  aspnet:9.0      (~200MB)  ← runtime only
Stage 2 (build)    →  sdk:9.0         (~800MB)  ← has compiler, NuGet
Stage 3 (publish)  →  continues build            ← optimized output
Stage 4 (final)    →  aspnet:9.0      (~200MB)  ← FINAL IMAGE (small!)
```

**Benefits:**
- Final image is ~200MB instead of ~800MB (no SDK/compiler included)
- Source code is NOT in the final image (security)
- Build tools are NOT in the final image (smaller attack surface)
- Docker caches each stage — rebuilds are fast

---

## 5. What is .dockerignore?

Just like `.gitignore` tells Git which files to skip, `.dockerignore` tells Docker which files to skip when running `COPY . .`

Our `.dockerignore`:

```
**/bin           # compiled output (we build fresh inside Docker)
**/obj           # intermediate build files
**/.vs           # Visual Studio settings
**/.vscode       # VS Code settings
**/node_modules  # not applicable but good practice
**/*.user        # user-specific settings
**/Thumbs.db     # Windows thumbnail cache
**/.DS_Store     # macOS folder metadata
**/docs          # documentation (not needed in container)
**/*.md          # markdown files (not needed in container)
```

**Why?**
- Keeps the Docker build context small → faster builds
- Prevents unnecessary files from ending up in the image
- Avoids overwriting freshly compiled code with stale local builds

---

## 6. What is Docker Compose?

Docker Compose is a tool that lets you define and run **multiple containers** with a single command using a YAML file.

Without Compose, you'd need to run 8 separate `docker run` commands with long arguments. With Compose:

```bash
docker compose up --build    # starts everything
docker compose down          # stops everything
```

---

## 7. docker-compose.yml Step-by-Step Breakdown

### Overall Structure

```yaml
services:       # define all containers
  rabbitmq:     # container 1
  consul:       # container 2
  api-gateway:  # container 3
  ...           # containers 4-8

networks:       # define shared network
  ecommerce-net:
```

---

### 7.1 RabbitMQ (Message Broker)

```yaml
rabbitmq:
    image: rabbitmq:3-management
    container_name: rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 10s
      timeout: 5s
      retries: 10
    networks:
      - ecommerce-net
```

| Property | What it does |
|----------|-------------|
| `image: rabbitmq:3-management` | Use the official RabbitMQ image with the management UI plugin (web dashboard). We don't build this — we pull it from Docker Hub. |
| `container_name: rabbitmq` | Give the container a fixed name. Other services use this name as the hostname (e.g., `rabbitmq:5672`). |
| `ports: "5672:5672"` | Map port 5672 (AMQP protocol) from container to host. Format: `host:container`. |
| `ports: "15672:15672"` | Map port 15672 (Management UI). Visit `http://localhost:15672` to see the dashboard. |
| `environment` | Set default username/password for RabbitMQ. |
| `healthcheck` | Docker pings RabbitMQ every 10 seconds to check if it's ready. Other services wait for this before starting. |
| `networks` | Connect to our custom `ecommerce-net` network so all services can talk to each other. |

---

### 7.2 Consul (Service Discovery)

```yaml
consul:
    image: consul:1.15
    container_name: consul
    ports:
      - "8500:8500"
    command: agent -server -bootstrap-expect=1 -ui -client=0.0.0.0
    networks:
      - ecommerce-net
```

| Property | What it does |
|----------|-------------|
| `image: consul:1.15` | Official Consul image for service discovery. |
| `ports: "8500:8500"` | Consul UI is available at `http://localhost:8500`. |
| `command` | Override the default startup command. `-server` runs it in server mode, `-bootstrap-expect=1` means single-node cluster, `-ui` enables web UI, `-client=0.0.0.0` accepts connections from any IP. |

---

### 7.3 API Gateway

```yaml
api-gateway:
    build:
      context: .
      dockerfile: src/ApiGateway/Dockerfile
    container_name: api-gateway
    ports:
      - "5000:5000"
    depends_on:
      - product-service
      - product-detail_service
      - cart.service
      - order-orchestrator-service
      - notification-service
    networks:
      - ecommerce-net
```

| Property | What it does |
|----------|-------------|
| `build: context: .` | Build from our source code. `context: .` means the build context is the project root (so `COPY . .` in Dockerfile copies everything from root). |
| `dockerfile: src/ApiGateway/Dockerfile` | Path to the Dockerfile relative to the context. |
| `ports: "5000:5000"` | The gateway listens on port 5000. This is the ONLY entry point for clients. |
| `depends_on` | Start this container AFTER all 5 microservices have started (but doesn't wait for them to be "healthy" — just "started"). |

---

### 7.4 Microservices (Product, ProductDetail, Cart)

```yaml
product-service:
    build:
      context: .
      dockerfile: src/ProductService/Dockerfile
    container_name: product-service
    ports:
      - "5001:5001"
    networks:
      - ecommerce-net
```

These three services (Product, ProductDetail, Cart) follow the same simple pattern:
- Build from their own Dockerfile
- Expose their own port
- Join the shared network
- No special dependencies needed — they are independent

| Service | Port |
|---------|------|
| Product Service | 5001 |
| Product Detail Service | 5002 |
| Cart Service | 5003 |

---

### 7.5 Order Orchestrator Service (with dependencies)

```yaml
order-orchestrator-service:
    build:
      context: .
      dockerfile: src/OrderOrchestratorService/Dockerfile
    container_name: order-orchestrator-service
    ports:
      - "5004:5004"
    environment:
      - ServiceUrls__CartService=http://cart.service:5003
      - ServiceUrls__ProductDetailService=http://product-detail_service:5002
      - RabbitMQ__Host=rabbitmq
    depends_on:
      rabbitmq:
        condition: service_healthy
    networks:
      - ecommerce-net
```

| Property | What it does |
|----------|-------------|
| `environment: ServiceUrls__CartService=...` | Override `appsettings.json` values. The `__` (double underscore) maps to `:` in .NET config (e.g., `ServiceUrls:CartService`). Uses container names as hostnames (not `localhost`!). |
| `environment: RabbitMQ__Host=rabbitmq` | Tell the service to connect to the `rabbitmq` container instead of `localhost`. |
| `depends_on: rabbitmq: condition: service_healthy` | Wait until RabbitMQ's healthcheck passes before starting. This is critical — if Order Service starts before RabbitMQ is ready, it can't publish events. |

**Why does this service need environment variables but others don't?**
- Order Orchestrator **calls other services** (Cart, ProductDetail) via HTTP and **publishes to RabbitMQ**
- Inside Docker, `localhost` doesn't work — each container is its own machine. You must use the `container_name` as the hostname.

---

### 7.6 Notification Service

```yaml
notification-service:
    build:
      context: .
      dockerfile: src/NotificationService/Dockerfile
    container_name: notification-service
    ports:
      - "5005:5005"
    environment:
      - RabbitMQ__Host=rabbitmq
    depends_on:
      rabbitmq:
        condition: service_healthy
    networks:
      - ecommerce-net
```

| Property | What it does |
|----------|-------------|
| `RabbitMQ__Host=rabbitmq` | Connects to RabbitMQ to consume order events. |
| `depends_on: rabbitmq: condition: service_healthy` | Waits for RabbitMQ to be healthy before starting (same as Order Orchestrator). |

---

### 7.7 Network

```yaml
networks:
  ecommerce-net:
    driver: bridge
```

| Property | What it does |
|----------|-------------|
| `ecommerce-net` | A custom Docker network that all containers join. |
| `driver: bridge` | Default network type. Containers on the same bridge network can communicate using container names as hostnames. |

**How containers talk to each other:**
```
cart.service → http://product-service:5001/api/products   (using container name)
order-orchestrator → http://cart.service:5003/api/cart     (using container name)
notification-service → amqp://rabbitmq:5672               (using container name)
```

---

## 8. How to Run Everything

### Step 1: Make sure Docker Desktop is running

### Step 2: Build and start all containers

```bash
docker compose up --build
```

| Flag | Meaning |
|------|---------|
| `up` | Create and start all containers defined in docker-compose.yml |
| `--build` | Force rebuild all images (use this when you change code) |

### Step 3: Verify everything is running

```bash
docker compose ps
```

Expected output:
```
NAME                        STATUS
rabbitmq                    running (healthy)
consul                      running
api-gateway                 running
product-service             running
product-detail_service      running
cart.service                running
order-orchestrator-service  running
notification-service        running
```

### Step 4: Access the services

| Service | URL |
|---------|-----|
| API Gateway (main entry) | http://localhost:5000 |
| RabbitMQ Dashboard | http://localhost:15672 (guest/guest) |
| Consul Dashboard | http://localhost:8500 |
| Product Service Swagger | http://localhost:5001/swagger |
| Product Detail Service Swagger | http://localhost:5002/swagger |
| Cart Service Swagger | http://localhost:5003/swagger |
| Order Orchestrator Swagger | http://localhost:5004/swagger |
| Notification Service Swagger | http://localhost:5005/swagger |

### Step 5: Stop everything

```bash
docker compose down
```

---

## 9. Common Docker Commands

| Command | What it does |
|---------|-------------|
| `docker compose up --build` | Build images and start all containers |
| `docker compose up -d` | Start in detached mode (runs in background) |
| `docker compose down` | Stop and remove all containers |
| `docker compose ps` | List running containers and their status |
| `docker compose logs -f` | Follow live logs from all services |
| `docker compose logs -f order-orchestrator-service` | Follow logs for one specific service |
| `docker compose restart cart.service` | Restart a single service |
| `docker compose build product-service` | Rebuild only one service's image |
| `docker images` | List all Docker images |
| `docker ps` | List all running containers |
| `docker exec -it product-service bash` | Open a shell inside a running container |

---

## 10. Troubleshooting

### Container can't connect to RabbitMQ
- **Cause:** Service started before RabbitMQ was ready.
- **Fix:** Make sure `depends_on` with `condition: service_healthy` is set.

### "Connection refused" between services
- **Cause:** Using `localhost` instead of container name.
- **Fix:** Inside Docker, use container names: `http://cart.service:5003`, NOT `http://localhost:5003`.

### Build fails with "file not found"
- **Cause:** Wrong `context` in docker-compose or wrong paths in Dockerfile.
- **Fix:** `context: .` should point to repo root. Dockerfile paths are relative to context.

### Changes not reflected after rebuild
- **Cause:** Docker cached old layers.
- **Fix:** Run `docker compose build --no-cache` to force fresh build.

### Port already in use
- **Cause:** Another process is using the same port on your machine.
- **Fix:** Stop the other process or change the host port in `ports` (e.g., `"5010:5001"`).

---

## Visual Summary

```
Your Machine (Host)
│
├── docker compose up --build
│
├── ecommerce-net (Docker Bridge Network)
│   │
│   ├── rabbitmq:5672          ← Message Broker
│   ├── consul:8500            ← Service Discovery
│   │
│   ├── api-gateway:5000       ← Single Entry Point
│   │   ├── routes to → product-service:5001
│   │   ├── routes to → product-detail_service:5002
│   │   ├── routes to → cart.service:5003
│   │   ├── routes to → order-orchestrator-service:5004
│   │   └── routes to → notification-service:5005
│   │
│   ├── product-service:5001
│   ├── product-detail_service:5002
│   ├── cart.service:5003
│   ├── order-orchestrator-service:5004
│   │   ├── calls → cart.service (HTTP)
│   │   ├── calls → product-detail_service (HTTP)
│   │   └── publishes → rabbitmq (AMQP)
│   │
│   └── notification-service:5005
│       └── consumes ← rabbitmq (AMQP)
│
└── Exposed Ports: 5000-5005, 8500, 5672, 15672
```
