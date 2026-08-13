# ThriftFlow Second-hand Clothing POS & Inventory System — Backend

> 🌐 **Language:** [English](README-en.md) | [ภาษาไทย](README.md)

Backend system for ThriftFlow, a Point of Sale (POS), Inventory Management, and Sales Analytics system tailored for second-hand clothing stores. Built with ASP.NET Core (C#), PostgreSQL (RDB), and Supabase Storage (Blob). This capstone project was developed to solve the inventory management challenges faced by second-hand stores, where items are often unique (one-of-a-kind) and frequently sold across multiple locations, requiring an efficient and convenient management solution. This API connects with the frontend client.

**Frontend repository:** [ThriftFlow-Frontend](https://github.com/phantoyooburee/Frontend_ThirftFlow.git)

**Live Demo:** [https://thriftflow.vercel.app](https://thriftflow.vercel.app)

![.NET](https://img.shields.io/badge/.NET-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![Entity Framework](https://img.shields.io/badge/Entity_Framework-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?style=for-the-badge&logo=postgresql&logoColor=white)
![Supabase](https://img.shields.io/badge/Supabase-3ECF8E?style=for-the-badge&logo=supabase&logoColor=white)
![JWT](https://img.shields.io/badge/JWT-000000?style=for-the-badge&logo=jsonwebtoken&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)

---
## Table of Contents

- [About the Project](#about-the-project)
- [Key Features](#key-features)
- [System Architecture](#system-architecture)
- [Tech Stack](#tech-stack)
- [System Requirements](#system-requirements)
- [Installation & Setup](#installation--setup)
- [How to Use](#how-to-use)
- [API Documentation](#api-documentation)
- [Database Structure](#database-structure)
- [Architecture Decisions](#architecture-decisions)
- [Security](#security)
- [Testing](#testing)
- [License](#license)
- [Developer](#developer)

---

## About the Project

Second-hand clothing stores often face more complex inventory management issues than typical retail stores because each item is usually unique (one-of-a-kind). The rapid turnover of goods makes manual tracking or spreadsheets prone to errors and time-consuming. Additionally, sales frequently occur across multiple locations on any given day.

**ThriftFlow Backend** is a RESTful API developed to address this problem. It covers everything from Point of Sale (POS), individual/bulk item inventory management, user/role management, to an automated promotion engine. It is designed to support sales from multiple branches using a centralized inventory while separately tracking sales records and tracing which branch sold each item.

**Why this tech stack was chosen:**

- **ASP.NET Core**: For type-safety and high performance suitable for a fast-responding POS system.
- **PostgreSQL**: A highly stable relational database ideal for systems with numerous stock and financial transactions.
- **Entity Framework Core (EF Core)**: Used as the primary ORM for CRUD operations, enabling rapid Code-First database design.
- **Supabase Storage**: Used for storing product images instead of saving them directly in the database, improving performance and reducing database size.
- **JWT + BCrypt**: For secure session and password management.
- **Swagger**: For API testing and serving as API documentation.

See the UI and actual usage at the [Frontend repository](https://github.com/phantoyooburee/Frontend_ThirftFlow.git)

## Key Features

### Point of Sale (POS)
- Fast checkout supporting Barcode scanning.
- Automatic calculation of prices, discounts, and promotions strictly on the backend (preventing price tampering from the client).
- Supports multi-branch sales, separating sales records by branch.

### Inventory Management
- 3-Dimensional product categorization: Category, ProductLot, and IsGenericSKU.
- Separates items with unique barcodes (Tagged Items) from bulk items sold in lots (Bulk Items).
- Stock adjustments with full audit trails.

### User & Access Management
- Role-based Access Control: Owner / Manager / Staff.
- Multi-Branch management within a single store, separating sales records and operations while sharing a single database.
- Comprehensive authentication: Register, Login, Forgot/Reset Password (JWT + BCrypt).

### Promotion Engine
- Auto-discovery of eligible promotions.
- Specificity-based tie-breaking for multiple matching conditions.
- Role-based control over price overrides.

### Payments
- Supported payment methods: Cash and Bank Transfer (transfers require uploading a slip image).
- Separate tracking of payment methods in receipts.
- Supports flat-rate pricing or custom agreed-upon prices.

### Sales Analytics
- Daily/Weekly/Monthly sales summaries.
- Reports separated by branch and employee.
- Best-selling and slow-moving product analysis.

## System Architecture
The system is designed using an **N-Tier (Layered) Architecture** via a RESTful API to ensure ease of maintenance and future scalability:

```text
[ Client / Web Browser ]  (Frontend: React/Vite)
          │
          ▼  (HTTP / REST API)
          │
[ Backend API Server ]    (ASP.NET Core 8)
          │
          ├───────────────┐
          ▼               ▼
[ PostgreSQL Database ] [ Supabase Storage ]
 (Stores all system data) (Stores images and payment slips)
```

## Tech Stack
- **Runtime/Framework**: C# / ASP.NET Core
- **Database**: PostgreSQL
- **Data Access**: Entity Framework Core
- **Authentication**: JWT Bearer, BCrypt
- **File Storage**: Supabase Storage
- **API Documentation**: Swagger / OpenAPI
- **Email**: Gmail SMTP (For Invite Employee, Forgot Password features)

---

## System Requirements
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL (Any version supported by Npgsql)
- Supabase account (For File Storage)
- Gmail App Password (For Invite Employee and Forgot Password features)
- Docker (If running via Container)

---

## Installation & Setup

### 1. Clone the repository

```bash
git clone https://github.com/phantoyooburee/Backend_ThriftFlowSystem.git

cd Backend_ThriftFlowSystem
```
### 2. Configure Environment Variables
Required configurations in `appsettings.json` (or `appsettings.example.json`):

```json
{
  "Jwt": {
    "Key": "YOUR_KEY",
    "Issuer": "YOUR_ISSUER",
    "Audience": "YOUR_AUDIENCE",
    "ExpiresMinutes": 480
  },
  "Email": {
    "Port": 587,
    "Password": "YOUR_EMAIL_PASSWORD",
    "Host": "smtp.gmail.com",
    "From": "YOUR_EMAIL"
  },
  "ConnectionStrings": {
    "DBContext": "Host=localhost;Port=5432;Database=ThriftFlowDb;Username=postgres;Password=YOUR_PASSWORD;"
  },
  "Supabase": {
    "Url": "YOUR_SUPABASE_URL",
    "Key": "YOUR_SUPABASE_KEY"
  },
  "App": {
    "BaseUrl": "http://localhost:5173"
  }
}
```

It is recommended to store sensitive secrets (`Jwt:Key`, `Email:Password`, `ConnectionStrings:DBContext`, `Supabase:Url`, `Supabase:Key`) using [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) instead of writing them directly to the file:
```bash
dotnet user-secrets init
dotnet user-secrets set "Jwt:Key" "your_secret_key"
dotnet user-secrets set "Email:Password" "your_gmail_app_password"
dotnet user-secrets set "ConnectionStrings:DBContext" "Host=localhost;Port=5432;Database=ThriftFlowDb;Username=postgres;Password=your_password;"
dotnet user-secrets set "Supabase:Url" "your_supabase_url"
dotnet user-secrets set "Supabase:Key" "your_supabase_key"
```
> This method safely keeps secrets out of the repository.

### 3. Restore Dependencies
```bash
dotnet restore
```

### 4. Run Migrations
```bash
dotnet ef database update
```

### 5. Run the Server
```bash
dotnet run
```
---

## How to Use
You can access the API at `http://localhost:[YourPort]` and test endpoints via Swagger at `http://localhost:[YourPort]/swagger/`

### Getting Started (First Time)
**1. Check system status (Check if initialized)**
```http
GET /api/auth/system-status
```
> If the response is `"isInitialized": false`, the system has no users yet.

**2. Register Owner Account (Only available when the system is empty)**
```http
POST /api/auth/register
Content-Type: application/json
{
  "email": "user@example.com",
  "username": "your_username",
  "password": "your_password",
  "pin": "your_pin",
  "firstName": "your_first_name",
  "lastName": "your_last_name"
}
```

**3. Login to get JWT Token**
```http
POST /api/auth/login
Content-Type: application/json

{
  "username": "owner@example.com",
  "password": "your_password"
}
```
> `username` can be either Email or Username.

### Adding Other Employees (Owner/Manager Only)
**4. Invite employee via Email (Requires JWT Token)**
```http
POST /api/auth/invite
Authorization: Bearer {your_jwt_token}
Content-Type: application/json

{
   "email": "[EMAIL_ADDRESS]",
   "roleId": 1
}
```
> `roleId` 1 = Owner, 2 = Manager, 3 = Staff
>> The system will send an Invitation Token to the employee's email.

**5. Employee registers using Invitation Token**

```http
POST /api/auth/register
Content-Type: application/json
{
  "invitationToken": "string",
  "email": "user@example.com",
  "username": "your_username",
  "password": "your_password",
  "pin": "your_pin",
  "firstName": "your_first_name",
  "lastName": "your_last_name"
}
```
> After registering, they can login and use the token for other APIs.

**6. Use Token for other APIs**
```http
GET /api/products
Authorization: Bearer {your_jwt_token}
```

### Order Management and POS (Point of Sale)

**7.1 Open Shift**
```http
POST /api/pos/shift/open
Authorization: Bearer {your_jwt_token}
Content-Type: application/json
{
  "branchId": 1,
  "startingCash": 500
}
```
> Must be done every morning before selling — `startingCash` is the initial cash drawer amount.

**7.2 Close Shift**
```http
POST /api/pos/shift/close
Authorization: Bearer {your_jwt_token}
Content-Type: application/json
{
  "branchId": 1,
  "endingCash": 500
}
```
> Must be done at closing — `endingCash` is the actual cash in the drawer. The system calculates discrepancies and records all shift sales.

**8. Calculate Cart Before Checkout (Validates prices and promotions)**
```http
POST /api/pos/calculate-cart
Authorization: Bearer {your_jwt_token}
Content-Type: application/json
{
  "items": [
    { "productId": 1, "quantity": 2 },
    { "productId": 5, "quantity": 1 }
  ],
  "skipPromotion": false,
  "specialPrice": null
}
```
**9. Checkout (Process payment and record sale)**
```http
POST /api/pos/checkout
Authorization: Bearer {your_jwt_token}
Content-Type: multipart/form-data

paymentMethod: CASH
cashReceived: 500
branchId: 1
orderItemsJson: [{"productId":1,"quantity":2},{"productId":5,"quantity":1}]
slipImage: (Attach slip image file — For TRANSFER only)
```
> If `paymentMethod` is TRANSFER, `slipImage` is required.
> If a special custom price is needed, send `specialPrice` and `managerPin`.

**10. Search Order by Receipt Number**
```http
GET /api/pos/orders/search?receiptNumber=TF-20260806-001
Authorization: Bearer {your_jwt_token}
```

### Inventory Management
**11. Add Supplier (Owner Only)**
```http
POST /api/inventory/suppliers
Authorization: Bearer {your_jwt_token}
Content-Type: application/json
{
  "name": "Chatuchak Vendor",
  "contactInfo": "086-xxx-xxxx"
}
```
**12. Create Product Lot (Required before adding products)**
```http
POST /api/inventory/product-lots
Authorization: Bearer {your_jwt_token}
Content-Type: application/json
{
  "lotName": "Lot June 2026",
  "supplierId": 1,
  "colorTag": "Green Tag",
  "totalLotCost": 3000,
  "receivedQuantity": 50
}
```

**13. Add New Product (To existing or new lot)**
```http
POST /api/inventory/products
Authorization: Bearer {your_jwt_token}
Content-Type: multipart/form-data
{
categoryId: 1
productLotId: 1
name: "Vintage White T-Shirt"
sellingPrice: 250
initialQuantity: 1
isGenericSKU: false
neckTag: " M "
width: 52
length: 70
detail: " Good condition " 
imageFile: (Attach product image file)
}
```
> `isGenericSKU: false` = Unique item with a specific barcode.
>> `isGenericSKU: true` = Bulk items sold in lots.

**14. Adjust Stock (Owner/Manager Only)**
```http
POST /api/inventory/adjust-stock
Authorization: Bearer {your_jwt_token}
X-PIN: {manager_pin}
Content-Type: application/json

{
  "productId": 1,
  "quantity": -1,
  "actionType": "DAMAGED",
  "note": "Item damaged, removed from stock"
}
```

## API Documentation

After running the server, access the Swagger UI at:
```
https://localhost:[YourPort]/swagger
```
### Core Endpoints
- **Authentication**:
  - `POST /api/auth/register` (Register store or via invite)
  - `POST /api/auth/login` (Login to receive Token)
  - `POST /api/auth/invite` (Owner creates invite links)
- **Inventory**:
  - `POST /api/inventory/product-lots` (Create incoming product lots)
  - `POST /api/inventory/products` (Add new products with images)
  - `POST /api/inventory/adjust-stock` (Adjust stock levels, e.g., damaged/lost)
- **POS**:
  - `POST /api/pos/shift/open` (Open daily shift)
  - `POST /api/pos/calculate-cart` (Calculate net price before payment)
  - `POST /api/pos/checkout` (Pay, deduct stock, record sale)
  - `POST /api/pos/shift/close` (Close shift, reconcile cash)
---

## Database Structure
The system uses Entity Framework Core (Code-First) for database design. Main tables include:

![Database ER Diagram](./Backend_ThriftFlowSystem/docs/er-diagram.png)
- **`Employees` & `Roles`**: Manages staff and permissions (Owner, Manager, Staff).
- **`Products` & `ProductLots`**: Manages both unique and bulk items, tracking costs from lots.
- **`Orders` & `OrderItems`**: Records sales history, receipts, and sold item details.
- **`POSShifts`**: Manages shift opening/closing and drawer cash tracking per branch.
- **`InventoryLogs`**: Records every stock movement for Audit Trails.

---
## Architecture Decisions

Technical decisions highlighted for architectural understanding:

- **N-Tier (Layered) Architecture**: The system cleanly separates `Controllers`, `Services`, `DTOs`, and `Data` (Repositories). This mitigates Fat Controllers, simplifies unit testing, and eases future scalability.
- **Multi-Branch Design**: Designed as a Single Store, Multiple Branches system. The `BranchId` is tied to the `POSShift`, allowing each branch to independently open/close shifts and reconcile sales without data conflicts.
- **Hybrid Product Tracking (IsGenericSKU)**: Second-hand stores have both unique items and bulk items. The `IsGenericSKU` boolean enforces that unique items (false) never exceed 1 in stock, preventing duplicate sales.
- **Promotion & Pricing Security**: All net price, discount, and promotion calculation logic is strictly located in the `POSServices` on the backend (via the `calculate-cart` endpoint). This prevents the Frontend from sending tampered net prices to the database.
- **Cost-Effective Payment Flow**: Connecting to Bank APIs (Payment Gateways) is costly for small stores. The system is designed to support slip image uploads (`UploadSlipLater`) as database proof, saving costs and reducing system complexity.
---

## Security
- **JWT Authentication** (Access Token) for API authorization.
- **Action PIN (X-PIN)** requires Managers/Owners to enter a 6-digit PIN for sensitive actions (e.g., voiding bills, adjusting stock) to prevent fraud.
- **Password Hashing** uses `BCrypt` to securely hash passwords.
- **Role-based Access Control (RBAC)** strictly separates access levels (Owner, Manager, Staff).
- **Input Validation** prevents erroneous data upfront using Data Annotations in Models/DTOs.

---
## Testing

Backend testing can currently be performed via the Swagger UI:

1. Run the project via Visual Studio or `dotnet run` in the terminal.
2. Open your browser to `http://localhost:[YourPort]/swagger`.
3. In any endpoint, click **"Try it out"**.
4. For secured APIs, login first to get a token and paste it into the **"Authorize"** button (Type `Bearer {token}`).
5. Test the requests and observe the responses in real-time.
**6. Use the demo accounts below to login via `POST /api/auth/login` to get a Token.**

### Demo Access
| Role | Username | Password | Manager PIN |
| :--- | :--- | :--- | :--- |
| **Owner** | `demo_owner` | `password123` | `123456` |
| **Manager** | `demo_manager` | `password123` | `654321` |
| **Staff** | `demo_staff` | `password123` | `121234` |

---

## License

This project is licensed under the **MIT License**.
You are free to study, run, modify, and build upon the source code (See the [LICENSE](LICENSE) file for more details).

---

## Developer

**Phanto Yooburee**
- GitHub: [phantoyooburee](https://github.com/phantoyooburee)
- LinkedIn: [Phanto Yooburee](https://www.linkedin.com/in/yooburee-phanto)

---

*View Frontend at [ThriftFlow-Frontend](https://github.com/phantoyooburee/ThriftFlow-Frontend)*
