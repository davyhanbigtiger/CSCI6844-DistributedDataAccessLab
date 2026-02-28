# CSCI6844 Midterm – Distributed E-Commerce Backend

A microservices backend for a simplified e-commerce platform.  
Built with ASP.NET Core 9, EF Core, Docker Compose, and RabbitMQ.

---

## What This Is

4 services, each in its own container, each with its own database.  
Services talk to each other via HTTP (sync) and RabbitMQ (async).  
No shared databases. No direct cross-service DB access.

---

## Services

| Service             | Port  | Database       | Role                             |
|---------------------|-------|----------------|----------------------------------|
| CustomerService     | 5001  | customers.db   | CRUD for customers               |
| OrderService        | 5002  | orders.db      | Create orders, validate via HTTP |
| ProductService      | 5003  | products.db    | CRUD for products, stock updates |
| NotificationService | 5004  | none           | Listens to RabbitMQ, logs events |

---

## How to Run

Make sure Docker Desktop is running, then:

    git clone <repo-url>
    cd DistributedDataAccessLab
    docker compose up --build

First build takes 2-3 min (NuGet restore inside containers).  
Wait until you see all four services say `Now listening on: http://[::]:8080`.

---

## Swagger URLs

    http://localhost:5001/swagger   # Customers
    http://localhost:5002/swagger   # Orders
    http://localhost:5003/swagger   # Products
    http://localhost:15672          # RabbitMQ UI  (guest / guest)

---

## Test Flow

Run these in order:

**1. Create a customer**

    POST http://localhost:5001/api/Customers
    { "name": "Davy Han", "email": "davy@example.com" }

**2. Create a product**

    POST http://localhost:5003/api/Products
    { "name": "MacBook Pro", "stock": 100 }

**3. Place an order**

    POST http://localhost:5002/api/Orders
    { "customerId": 1, "productId": 1, "quantity": 2 }

**4. Check stock dropped**

    GET http://localhost:5003/api/Products/1
    → stock should be 98

**5. Check NotificationService logs**

    docker logs notificationservice
    → [Notification] Order received: OrderId=1, ProductId=1, Qty=2

---

## Project Structure

    DistributedDataAccessLab/
    ├── CustomerService/
    │   └── CustomerService.Api/
    │       ├── Controllers/
    │       ├── Data/               # CustomerDbCon.cs
    │       └── Migrations/
    │
    ├── ProductService/
    │   └── ProductService.Api/
    │       ├── Controllers/
    │       ├── Consumers/          # RabbitMQ consumer
    │       └── Data/               # ProductDbCon.cs
    │
    ├── OrderService/
    │   └── OrderService.Api/
    │       ├── Controllers/
    │       ├── Services/           # CustomerClient, ProductClient, OrderPublisher
    │       └── Data/               # OrderDbCon.cs
    │
    ├── NotificationService/
    │   └── NotificationService.Api/
    │       └── Consumers/          # RabbitMQ consumer, stateless
    │
    ├── data/                       # SQLite files (volume mounts)
    │   ├── customers/
    │   ├── products/
    │   └── orders/
    │
    └── docker-compose.yml

---

## Things I Ran Into

- **Old .db files getting baked into the image** — fixed with `.dockerignore`
- **`/app/data` not existing at startup** — added `Directory.CreateDirectory()` in Program.cs before `EnsureCreated()`
- **Cross-service calls failing** — was using `localhost`, should be `customerservice:8080` (Docker internal DNS)

---

## Tech Stack

- ASP.NET Core 9
- EF Core 9 with SQLite
- RabbitMQ 3.13
- Docker + Docker Compose
- Swagger / Swashbuckle
