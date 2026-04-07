# Use Case Diagram

## Who can do what?

```mermaid
graph LR
    Admin((👤 Admin))
    User((👤 User))

    Admin --> PM
    Admin --> PDM
    User --> PB
    User --> SC
    User --> CO

    subgraph PM["🏪 Manage Products"]
        A1["Add Product"]
        A2["Remove Product"]
    end

    subgraph PDM["🏷️ Manage Product Details"]
        A3["Add Size / Price / Design"]
        A4["Remove Details"]
    end

    subgraph PB["🔍 Browse Products"]
        B1["View All Products"]
        B2["View Product Details"]
    end

    subgraph SC["🛒 Shopping Cart"]
        C1["Add Item to Cart"]
        C2["View Cart"]
        C3["Remove Item"]
    end

    subgraph CO["💳 Checkout"]
        D1["Place Order"]
        D2["View My Orders"]
    end

    D1 -.->|sends notification| N1

    subgraph N["🔔 Notifications"]
        N1["Order Confirmation"]
        N2["Order Failed Alert"]
    end
```

> **Admin** = manages the store (add/remove products and their details)
>
> **User** = shops (browse, cart, checkout)
>
> Both Admin and User need to **login first** to get a token

---

## Simple Summary

| Who? | What can they do? | Needs Login? |
|------|-------------------|:------------:|
| Admin | Add / Remove products | Yes |
| Admin | Add / Remove product details (size, price, design) | Yes |
| Anyone | View all products & details | No |
| User | Add items to cart, view cart, remove items | Yes |
| User | Place an order (checkout) | Yes |
| User | View past orders | Yes |
| System | Send order notifications automatically | — |
