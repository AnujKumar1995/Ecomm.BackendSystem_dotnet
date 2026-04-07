# CI/CD Pipeline Diagram

## Pipeline Overview

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                           CI/CD PIPELINE                                      │
│                        (GitHub Actions / Azure DevOps)                        │
└──────────────────────────────────────────────────────────────────────────────┘

 ┌─────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐     ┌─────────┐
 │  SOURCE │────▶│  BUILD   │────▶│   TEST   │────▶│  DOCKER  │────▶│ DEPLOY  │
 │ CONTROL │     │  STAGE   │     │  STAGE   │     │  STAGE   │     │  STAGE  │
 └─────────┘     └──────────┘     └──────────┘     └──────────┘     └─────────┘

 ┌─────────────────────────────────────────────────────────────────────────────┐
 │ STAGE 1: SOURCE CONTROL (Trigger)                                           │
 │                                                                             │
 │  Developer ──push──▶ GitHub Repository                                      │
 │                         │                                                   │
 │                         ├── main branch    → Full Pipeline (Build+Test+     │
 │                         │                    Docker+Deploy)                  │
 │                         ├── develop branch → Build + Test + Docker           │
 │                         └── feature/*      → Build + Test only              │
 │                                                                             │
 │  Triggers: push to main/develop, pull_request to main                       │
 └─────────────────────────────────────────────────────────────────────────────┘

 ┌─────────────────────────────────────────────────────────────────────────────┐
 │ STAGE 2: BUILD                                                              │
 │                                                                             │
 │  For EACH microservice (parallel jobs):                                     │
 │                                                                             │
 │  ┌──────────────────────────────────────────┐                               │
 │  │  1. Checkout code                         │                               │
 │  │  2. Setup .NET 8 SDK                      │                               │
 │  │  3. dotnet restore (NuGet packages)       │                               │
 │  │  4. dotnet build --configuration Release  │                               │
 │  │  5. Upload build artifacts                │                               │
 │  └──────────────────────────────────────────┘                               │
 │                                                                             │
 │  Services built in parallel:                                                │
 │  ├── ApiGateway                                                             │
 │  ├── ProductService                                                         │
 │  ├── ProductDetailService                                                   │
 │  ├── CartService                                                            │
 │  ├── OrderOrchestratorService                                               │
 │  └── NotificationService                                                    │
 └─────────────────────────────────────────────────────────────────────────────┘

 ┌─────────────────────────────────────────────────────────────────────────────┐
 │ STAGE 3: TEST                                                               │
 │                                                                             │
 │  For EACH microservice (parallel jobs):                                     │
 │                                                                             │
 │  ┌──────────────────────────────────────────┐                               │
 │  │  1. dotnet test --collect:"Code Coverage" │                               │
 │  │  2. Run unit tests                        │                               │
 │  │  3. Run integration tests                 │                               │
 │  │  4. Publish test results                  │                               │
 │  │  5. Code coverage report                  │                               │
 │  └──────────────────────────────────────────┘                               │
 │                                                                             │
 │  Quality Gates:                                                             │
 │  ├── All unit tests pass                                                    │
 │  ├── Code coverage > 70%                                                    │
 │  └── No critical security vulnerabilities                                   │
 └─────────────────────────────────────────────────────────────────────────────┘

 ┌─────────────────────────────────────────────────────────────────────────────┐
 │ STAGE 4: DOCKER BUILD & PUSH                                                │
 │                                                                             │
 │  For EACH microservice (parallel jobs):                                     │
 │                                                                             │
 │  ┌──────────────────────────────────────────┐                               │
 │  │  1. docker build -t service:tag .         │                               │
 │  │  2. docker tag (version + latest)         │                               │
 │  │  3. docker push to Container Registry     │                               │
 │  │     (Docker Hub / ACR / ECR)              │                               │
 │  │  4. Scan image for vulnerabilities        │                               │
 │  └──────────────────────────────────────────┘                               │
 │                                                                             │
 │  Images:                                                                    │
 │  ├── ecommerce/api-gateway:1.0.0                                            │
 │  ├── ecommerce/product-service:1.0.0                                        │
 │  ├── ecommerce/product-detail_service:1.0.0                                 │
 │  ├── ecommerce/cart.service:1.0.0                                           │
 │  ├── ecommerce/order-orchestrator:1.0.0                                     │
 │  └── ecommerce/notification-service:1.0.0                                   │
 └─────────────────────────────────────────────────────────────────────────────┘

 ┌─────────────────────────────────────────────────────────────────────────────┐
 │ STAGE 5: DEPLOY                                                             │
 │                                                                             │
 │  ┌────────────────┐     ┌────────────────┐     ┌────────────────┐          │
 │  │   DEV / QA     │────▶│   STAGING      │────▶│  PRODUCTION    │          │
 │  │  (Auto-deploy) │     │ (Auto-deploy)  │     │ (Manual gate)  │          │
 │  └────────────────┘     └────────────────┘     └────────────────┘          │
 │                                                                             │
 │  Deployment steps:                                                          │
 │  ┌──────────────────────────────────────────┐                               │
 │  │  1. Pull latest images                    │                               │
 │  │  2. docker-compose down                   │                               │
 │  │  3. docker-compose up -d                  │                               │
 │  │  4. Health check all services             │                               │
 │  │  5. Run smoke tests                       │                               │
 │  │  6. Notify team (Slack/Teams)             │                               │
 │  └──────────────────────────────────────────┘                               │
 │                                                                             │
 │  Rollback: docker-compose with previous image tags                          │
 └─────────────────────────────────────────────────────────────────────────────┘
```

## GitHub Actions Workflow (Summary)

```yaml
# .github/workflows/ci-cd.yml
name: Ecommerce Microservices CI/CD

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main]

jobs:
  build-and-test:
    strategy:
      matrix:
        service: [ApiGateway, ProductService, ProductDetailService, 
                  CartService, OrderOrchestratorService, NotificationService]
    steps:
      - checkout
      - setup-dotnet 8.0
      - dotnet restore
      - dotnet build
      - dotnet test

  docker-build:
    needs: build-and-test
    steps:
      - docker build & push for each service

  deploy:
    needs: docker-build
    if: github.ref == 'refs/heads/main'
    steps:
      - docker-compose up on target environment
```
