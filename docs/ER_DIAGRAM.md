# ER Diagram - Ecommerce Microservices

> How our data is organized across all services.

---

## Data Model Overview

```mermaid
erDiagram
    USER ||--o| CART : "has one"
    USER ||--o{ ORDER : "places"
    PRODUCT ||--o{ PRODUCT_DETAIL : "has variants"
    CART ||--o{ CART_ITEM : "contains"
    ORDER ||--o{ ORDER_ITEM : "contains"
    ORDER ||--o{ NOTIFICATION : "triggers"

    USER {
        id guid
        email string
        role string
    }

    PRODUCT {
        id guid
        name string
        category string
    }

    PRODUCT_DETAIL {
        id guid
        size string
        price decimal
        color string
        stock int
    }

    CART {
        userId guid
        totalAmount decimal
    }

    CART_ITEM {
        productName string
        size string
        price decimal
        quantity int
    }

    ORDER {
        id guid
        totalAmount decimal
        status string
        createdAt datetime
    }

    ORDER_ITEM {
        productName string
        size string
        price decimal
        quantity int
    }

    NOTIFICATION {
        eventType string
        orderId guid
        timestamp datetime
    }
```

---

## Which Service Owns What?

```mermaid
graph LR
    subgraph "Product Service"
        A[Product]
    end
    subgraph "Product Detail Service"
        B[Product Detail]
    end
    subgraph "Cart Service"
        C[Cart + Cart Items]
    end
    subgraph "Order Service"
        D[Order + Order Items]
    end
    subgraph "Notification Service"
        E[Notifications]
    end

    A -.->|variants| B
    C -.->|checkout| D
    D -.->|event| E
```

---

## Simple Relationship Summary

| Entity | Connects To | How? |
|--------|-------------|------|
| **Product** | Product Detail | 1 product has many variants (sizes, colors) |
| **User** | Cart | 1 user has 1 cart |
| **Cart** | Cart Items | 1 cart has many items |
| **User** | Orders | 1 user can place many orders |
| **Order** | Order Items | 1 order has many items |
| **Order** | Notification | 1 order triggers notifications |

---

## Recommended Databases (for production)

| Service | Best Database | Why? |
|---------|--------------|------|
| Product Service | PostgreSQL | Structured data, filtering |
| Product Detail Service | PostgreSQL | Variant data, joins |
| Cart Service | Redis | Fast, temporary data |
| Order Service | PostgreSQL | Transactions, audit trail |
| Notification Service | MongoDB | Flexible event logs |
