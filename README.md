# BTK MockECommerce Platform - Technical Documentation Report

## Project Overview

**Project Name:** BTK MockECommerce (Market Hub)
**Project Type:** Full-Stack E-Commerce Platform
**Development Team:** OOP Course Project
**Architecture:** Client-Server Model with RESTful API

This document provides comprehensive technical documentation for the BTK MockECommerce platform, a sophisticated e-commerce solution featuring dual user roles (Customer and Seller), product management, order processing, and secure authentication mechanisms.

---

## Table of Contents

1. [System Architecture](#1-system-architecture)
2. [Backend Technical Documentation](#2-backend-technical-documentation)
3. [Frontend Technical Documentation](#3-frontend-technical-documentation)
4. [User Interface & Navigation Guide](#4-user-interface--navigation-guide)
5. [Functional Requirements](#5-functional-requirements)
6. [Non-Functional Requirements](#6-non-functional-requirements)
7. [Database Design](#7-database-design)
8. [API Documentation](#8-api-documentation)
9. [Security Implementation](#9-security-implementation)
10. [Design Patterns Used](#10-design-patterns-used)

---

## 1. System Architecture

### 1.1 High-Level Architecture Diagram

```
+------------------------------------------------------------------+
|                         CLIENT LAYER                              |
|  +--------------------------------------------------------------+ |
|  |              React 18 + TypeScript + Vite                     | |
|  |  +------------+  +------------+  +------------+               | |
|  |  |   Pages    |  | Components |  |  Contexts  |               | |
|  |  | (17 views) |  |  (51+ UI)  |  | (Auth/Cart)|               | |
|  |  +------------+  +------------+  +------------+               | |
|  |                        |                                      | |
|  |           +------------+------------+                         | |
|  |           |   React Query + API     |                         | |
|  |           |     Client Layer        |                         | |
|  |           +------------+------------+                         | |
|  +------------------------|------------------------------------- + |
+---------------------------|---------------------------------------+
                            | HTTPS/REST
                            v
+------------------------------------------------------------------+
|                         SERVER LAYER                              |
|  +--------------------------------------------------------------+ |
|  |              ASP.NET Core 8.0 Web API                         | |
|  |  +----------------------------------------------------------+ | |
|  |  |                 PRESENTATION LAYER                        | | |
|  |  | Controllers | Middlewares | Filters | CORS | Rate Limit  | | |
|  |  +----------------------------------------------------------+ | |
|  |                            |                                  | |
|  |  +----------------------------------------------------------+ | |
|  |  |                   BUSINESS LAYER                          | | |
|  |  | Services | Managers | Validators | AutoMapper | Exceptions| | |
|  |  +----------------------------------------------------------+ | |
|  |                            |                                  | |
|  |  +----------------------------------------------------------+ | |
|  |  |                  DATA ACCESS LAYER                        | | |
|  |  | Entity Framework Core | Repositories | DbContext | Entities| | |
|  |  +----------------------------------------------------------+ | |
|  +--------------------------------------------------------------+ |
+---------------------------|---------------------------------------+
                            |
                            v
+------------------------------------------------------------------+
|                        DATABASE LAYER                             |
|                  PostgreSQL Relational Database                   |
|        Users | Products | Categories | Orders | SellerProfiles    |
+------------------------------------------------------------------+
```

### 1.2 Technology Stack Summary

| Layer            | Technology                     | Version        |
| ---------------- | ------------------------------ | -------------- |
| Frontend         | React + TypeScript             | 18.3.1 / 5.5.3 |
| Build Tool       | Vite                           | 5.4.1          |
| UI Framework     | Tailwind CSS + shadcn/ui       | 3.4.11         |
| State Management | React Context + TanStack Query | 5.56.2         |
| Backend          | ASP.NET Core                   | 8.0            |
| ORM              | Entity Framework Core          | 8.0            |
| Database         | PostgreSQL                     | Latest         |
| Authentication   | JWT Bearer + API Key           | Custom         |

---

## 2. Backend Technical Documentation

### 2.1 Project Structure

The backend follows a clean **N-Tier Layered Architecture** with strict separation of concerns:

```
BTK-MockECommerce-Server/
|-- MockECommerce.WebAPI/           # Presentation Layer
|   |-- Controllers/                # REST API Endpoints
|   |   |-- AuthController.cs       # Authentication endpoints
|   |   |-- ProductController.cs    # Product management
|   |   |-- CategoryController.cs   # Category operations
|   |   |-- OrderController.cs      # Order processing
|   |   |-- ApiKeyController.cs     # API key management
|   |   +-- ExternalProductController.cs  # External API
|   |-- Middlewares/                # HTTP Pipeline Components
|   |   |-- ExceptionHandlingMiddleware.cs
|   |   +-- ApiKeyAuthenticationMiddleware.cs
|   |-- Extensions/                 # Startup Extensions
|   |-- Filters/                    # Action Filters
|   +-- Program.cs                  # Application Entry Point
|
|-- MockECommerce.BusinessLayer/    # Business Logic Layer
|   |-- Managers/                   # Service Implementations
|   |   |-- AuthManager.cs
|   |   |-- ProductManager.cs
|   |   |-- CategoryManager.cs
|   |   |-- OrderManager.cs
|   |   +-- ApiKeyManager.cs
|   |-- Services/                   # Service Interfaces
|   |-- Mapping/                    # AutoMapper Profiles
|   |-- Exceptions/                 # Custom Exceptions
|   +-- Utils/                      # Utility Classes
|
|-- MockECommerce.DAL/              # Data Access Layer
|   |-- Data/                       # DbContext
|   |-- Entities/                   # Domain Models
|   |-- Repositories/               # Data Repositories
|   |-- Abstract/                   # Repository Interfaces
|   +-- Enums/                      # Enumerations
|
+-- MockECommerce.DtoLayer/         # Data Transfer Objects
    |-- AuthDtos/
    |-- ProductDtos/
    |-- CategoryDtos/
    |-- OrderDtos/
    +-- ApiKeyDtos/
```

### 2.2 Core Technologies & Frameworks

| Technology                | Purpose           | Implementation Details                        |
| ------------------------- | ----------------- | --------------------------------------------- |
| **ASP.NET Core 8.0**      | Web Framework     | Minimal hosting model, middleware pipeline    |
| **Entity Framework Core** | ORM               | Code-first approach, migrations, LINQ queries |
| **ASP.NET Core Identity** | User Management   | Custom AppUser, roles, claims                 |
| **AutoMapper**            | Object Mapping    | Entity-DTO transformations                    |
| **PostgreSQL**            | Database          | Relational data storage with indexes          |
| **JWT Bearer**            | Authentication    | Token-based user authentication               |
| **Swagger/OpenAPI**       | API Documentation | Interactive API testing UI                    |

### 2.3 Middleware Pipeline

The HTTP request pipeline processes requests in the following order:

```csharp
// Pipeline Order (Program.cs)
1. app.UseDeveloperExceptionPage()    // Development only
2. app.UseSwagger()                   // API documentation
3. app.UseRouting()                   // Route matching
4. app.UseCors("VercelandDev")        // Cross-origin requests
5. app.UseMiddleware<ExceptionHandlingMiddleware>()  // Global error handling
6. app.UseRateLimiter()               // Request throttling
7. app.UseAuthentication()            // JWT validation
8. app.UseMiddleware<ApiKeyAuthenticationMiddleware>() // API key auth
9. app.UseAuthorization()             // Role-based access
10. app.MapControllers()              // Endpoint routing
```

### 2.4 Authentication System

#### JWT Bearer Authentication

- **Token Generation:** Symmetric key signing (HS256)
- **Token Lifetime:** Configurable (default 60 minutes)
- **Claims:** User ID, Email, Role, Custom claims
- **Validation:** Issuer, Audience, Signing key, Lifetime

#### API Key Authentication (External API)

- **Header:** `X-API-Key`
- **Format:** `mec_[base64-encoded-32-bytes]`
- **Scope:** `/api/v1/external/*` endpoints
- **Features:** Rate limiting, expiration, usage tracking

### 2.5 Rate Limiting Configuration

| Policy   | Limit        | Window     | Partition |
| -------- | ------------ | ---------- | --------- |
| Global   | 500 requests | 1 minute   | All users |
| Login    | 7 requests   | 5 minutes  | Per IP    |
| Register | 5 requests   | 30 minutes | Per IP    |

---

## 3. Frontend Technical Documentation

### 3.1 Project Structure

```
BTK-MockECommerce-Front/
+-- src/
    |-- pages/                      # Page Components (17 pages)
    |   |-- Index.tsx               # Home page
    |   |-- Products.tsx            # Product catalog
    |   |-- ProductDetail.tsx       # Product details
    |   |-- Cart.tsx                # Shopping cart
    |   |-- Checkout.tsx            # Payment flow
    |   |-- CheckoutSuccess.tsx     # Order confirmation
    |   |-- UserLogin.tsx           # Customer login
    |   |-- UserRegister.tsx        # Customer registration
    |   |-- SellerLogin.tsx         # Seller login
    |   |-- SellerRegister.tsx      # Seller registration
    |   |-- SellerDashboard.tsx     # Seller management
    |   |-- About.tsx               # About page
    |   |-- Contact.tsx             # Contact page
    |   +-- NotFound.tsx            # 404 page
    |
    |-- components/                 # Reusable Components
    |   |-- Header.tsx              # Navigation header
    |   |-- Footer.tsx              # Page footer
    |   |-- ProductCard.tsx         # Product display card
    |   |-- ProductList.tsx         # Product table
    |   |-- AddProductForm.tsx      # Product creation form
    |   |-- FeaturedProducts.tsx    # Featured section
    |   |-- Hero.tsx                # Landing hero
    |   |-- Categories.tsx          # Category filter
    |   |-- ProtectedRoute.tsx      # Auth guard
    |   |-- ErrorBoundary.tsx       # Error handling
    |   +-- ui/                     # 51 shadcn/ui components
    |
    |-- contexts/                   # State Management
    |   |-- AuthContext.tsx         # Authentication state
    |   +-- CartContext.tsx         # Shopping cart state
    |
    |-- hooks/                      # Custom Hooks
    |   |-- useApi.ts               # React Query hooks
    |   |-- use-toast.ts            # Notifications
    |   +-- use-mobile.tsx          # Responsive detection
    |
    |-- lib/                        # Utilities
    |   |-- api.ts                  # HTTP client
    |   |-- errorHandler.ts         # Error handling
    |   +-- utils.ts                # Helper functions
    |
    |-- App.tsx                     # Root component
    |-- main.tsx                    # Entry point
    +-- index.css                   # Global styles
```

### 3.2 State Management Architecture

#### AuthContext (Authentication State)

```typescript
interface AuthState {
  user: User | null; // Current user data
  token: string | null; // JWT token
  isAuthenticated: boolean; // Auth status
  isLoading: boolean; // Loading state
  error: string | null; // Error messages
}

// Available Actions
-login(email, password) - // User authentication
  registerUser(userData) - // Customer registration
  registerSeller(sellerData) - // Seller registration
  logout() - // Clear session
  clearError(); // Reset errors
```

#### CartContext (Shopping Cart State)

```typescript
interface CartState {
  items: CartItem[]; // Cart items array
  total: number; // Total price
  itemCount: number; // Item quantity
}

// Available Actions
-addToCart(item) - // Add item
  removeFromCart(id) - // Remove item
  updateQuantity(id, qty) - // Update quantity
  clearCart(); // Empty cart
```

### 3.3 Routing Configuration

| Route               | Component       | Auth Required | Role   |
| ------------------- | --------------- | ------------- | ------ |
| `/`                 | Index           | No            | -      |
| `/products`         | Products        | No            | -      |
| `/product/:id`      | ProductDetail   | No            | -      |
| `/cart`             | Cart            | Yes           | Any    |
| `/checkout`         | Checkout        | Yes           | Any    |
| `/checkout-success` | CheckoutSuccess | Yes           | Any    |
| `/login`            | UserLogin       | No            | -      |
| `/register`         | UserRegister    | No            | -      |
| `/seller-login`     | SellerLogin     | No            | -      |
| `/seller-register`  | SellerRegister  | No            | -      |
| `/seller/dashboard` | SellerDashboard | Yes           | Seller |
| `/about`            | About           | No            | -      |
| `/contact`          | Contact         | No            | -      |
| `*`                 | NotFound        | No            | -      |

---

## 4. User Interface & Navigation Guide

### 4.1 Header Navigation

The header component provides primary navigation across the platform:

| Button/Element        | Location           | Action | Navigation Target                                   |
| --------------------- | ------------------ | ------ | --------------------------------------------------- |
| **Logo (Market Hub)** | Top-left           | Click  | Navigates to Home (`/`)                             |
| **Products**          | Nav menu           | Click  | Navigates to Product Catalog (`/products`)          |
| **About**             | Nav menu           | Click  | Navigates to About Page (`/about`)                  |
| **Contact**           | Nav menu           | Click  | Navigates to Contact Page (`/contact`)              |
| **Search Icon**       | Top-right          | Click  | Opens search bar / Searches products                |
| **Cart Icon**         | Top-right          | Click  | Navigates to Shopping Cart (`/cart`)                |
| **User Avatar**       | Top-right          | Click  | Opens user dropdown menu                            |
| **Login**             | Dropdown/Nav       | Click  | Navigates to Login Page (`/login`)                  |
| **Register**          | Dropdown/Nav       | Click  | Navigates to Register Page (`/register`)            |
| **Seller Login**      | Dropdown           | Click  | Navigates to Seller Login (`/seller-login`)         |
| **Dashboard**         | Dropdown (Seller)  | Click  | Navigates to Seller Dashboard (`/seller/dashboard`) |
| **Logout**            | Dropdown           | Click  | Logs out user, redirects to Home                    |
| **Mobile Menu**       | Top-right (mobile) | Click  | Toggles mobile navigation drawer                    |

### 4.2 Home Page (Index)

| Element                      | Action | Result                                       |
| ---------------------------- | ------ | -------------------------------------------- |
| **Hero "Shop Now" Button**   | Click  | Navigates to Products (`/products`)          |
| **Hero "Learn More" Button** | Click  | Navigates to About (`/about`)                |
| **Featured Product Card**    | Click  | Navigates to Product Detail (`/product/:id`) |
| **"Add to Cart" Button**     | Click  | Adds product to cart (toast notification)    |
| **Category Cards**           | Click  | Filters products by category                 |
| **"View All Products"**      | Click  | Navigates to Products (`/products`)          |

### 4.3 Products Page

| Element                     | Action        | Result                                       |
| --------------------------- | ------------- | -------------------------------------------- |
| **Search Bar**              | Type + Enter  | Filters products by search term              |
| **Category Filter Buttons** | Click         | Filters products by category                 |
| **Price Range Slider**      | Drag          | Filters products by price range              |
| **Sort Dropdown**           | Select        | Sorts products (Price, Newest, Rating)       |
| **Grid View Icon**          | Click         | Displays products in grid layout             |
| **List View Icon**          | Click         | Displays products in list layout             |
| **Product Card**            | Click         | Navigates to Product Detail (`/product/:id`) |
| **Quick Add to Cart**       | Click (hover) | Adds product to cart                         |
| **Favorite Heart Icon**     | Click         | Adds to favorites (visual feedback)          |
| **Pagination Controls**     | Click         | Navigates between product pages              |

### 4.4 Product Detail Page

| Element                   | Action | Result                             |
| ------------------------- | ------ | ---------------------------------- |
| **Image Thumbnails**      | Click  | Changes main product image         |
| **Main Image**            | Click  | Opens image lightbox/zoom          |
| **Quantity Minus (-)**    | Click  | Decreases quantity (min: 1)        |
| **Quantity Plus (+)**     | Click  | Increases quantity (max: stock)    |
| **"Add to Cart" Button**  | Click  | Adds product with quantity to cart |
| **"Buy Now" Button**      | Click  | Adds to cart and goes to Checkout  |
| **Category Link**         | Click  | Navigates to category products     |
| **Seller Name Link**      | Click  | Shows seller's products            |
| **Share Buttons**         | Click  | Opens share dialog                 |
| **Review Stars**          | Click  | Scrolls to reviews section         |
| **"Write Review" Button** | Click  | Opens review form (auth required)  |
| **Related Products**      | Click  | Navigates to related product       |

### 4.5 Shopping Cart Page

| Element                   | Action     | Result                              |
| ------------------------- | ---------- | ----------------------------------- |
| **Quantity Input**        | Change     | Updates item quantity               |
| **Remove Item (X)**       | Click      | Removes item from cart              |
| **"Clear Cart" Button**   | Click      | Removes all items                   |
| **"Continue Shopping"**   | Click      | Navigates to Products (`/products`) |
| **"Proceed to Checkout"** | Click      | Navigates to Checkout (`/checkout`) |
| **Coupon Input**          | Enter code | Applies discount coupon             |
| **"Apply" Button**        | Click      | Validates and applies coupon        |

### 4.6 Checkout Page

| Element                  | Action | Result                               |
| ------------------------ | ------ | ------------------------------------ |
| **Shipping Form Fields** | Fill   | Captures delivery address            |
| **Payment Card Fields**  | Fill   | Captures payment information         |
| **"Same as Shipping"**   | Check  | Copies shipping to billing           |
| **"Place Order" Button** | Click  | Processes payment, creates order     |
| **Order Summary**        | View   | Displays items, subtotal, tax, total |
| **"Back to Cart"**       | Click  | Returns to Cart page                 |

### 4.7 Seller Dashboard

| Element                   | Action | Result                       |
| ------------------------- | ------ | ---------------------------- |
| **"Add New Product" Tab** | Click  | Shows product creation form  |
| **"My Products" Tab**     | Click  | Shows product list table     |
| **"Orders" Tab**          | Click  | Shows incoming orders        |
| **"API Keys" Tab**        | Click  | Shows API key management     |
| **Product Form Submit**   | Click  | Creates new product          |
| **Edit Product Icon**     | Click  | Opens product edit modal     |
| **Delete Product Icon**   | Click  | Confirms and deletes product |
| **Status Toggle**         | Click  | Changes product status       |
| **"Generate API Key"**    | Click  | Creates new API key          |
| **Copy API Key Icon**     | Click  | Copies key to clipboard      |
| **Deactivate Key**        | Click  | Disables API key             |

### 4.8 Authentication Pages

#### Login Page

| Element                  | Action | Result                      |
| ------------------------ | ------ | --------------------------- |
| **Email Input**          | Fill   | Captures user email         |
| **Password Input**       | Fill   | Captures user password      |
| **"Show Password" Icon** | Click  | Toggles password visibility |
| **"Remember Me"**        | Check  | Persists session            |
| **"Login" Button**       | Click  | Authenticates user          |
| **"Forgot Password"**    | Click  | Opens password recovery     |
| **"Create Account"**     | Click  | Navigates to Register       |
| **"Login as Seller"**    | Click  | Navigates to Seller Login   |

#### Register Page

| Element                    | Action | Result                    |
| -------------------------- | ------ | ------------------------- |
| **Form Fields**            | Fill   | Captures user information |
| **"Terms & Conditions"**   | Check  | Accepts terms             |
| **"Register" Button**      | Click  | Creates account           |
| **"Already have account"** | Click  | Navigates to Login        |

---

## 5. Functional Requirements

### 5.1 User Management (FR-UM)

| ID       | Requirement                                                                          | Priority | Status      |
| -------- | ------------------------------------------------------------------------------------ | -------- | ----------- |
| FR-UM-01 | System shall allow users to register with email, password, first name, and last name | High     | Implemented |
| FR-UM-02 | System shall allow sellers to register with additional store name and logo           | High     | Implemented |
| FR-UM-03 | System shall authenticate users using email and password                             | High     | Implemented |
| FR-UM-04 | System shall generate JWT tokens upon successful authentication                      | High     | Implemented |
| FR-UM-05 | System shall support three user roles: Customer, Seller, Admin                       | High     | Implemented |
| FR-UM-06 | System shall allow users to logout and invalidate their session                      | High     | Implemented |
| FR-UM-07 | System shall validate token on protected routes                                      | High     | Implemented |
| FR-UM-08 | System shall redirect unauthenticated users to login page                            | Medium   | Implemented |

### 5.2 Product Management (FR-PM)

| ID       | Requirement                                                                                | Priority | Status      |
| -------- | ------------------------------------------------------------------------------------------ | -------- | ----------- |
| FR-PM-01 | Sellers shall be able to create products with title, description, price, stock, and images | High     | Implemented |
| FR-PM-02 | Sellers shall be able to update their own products                                         | High     | Implemented |
| FR-PM-03 | Sellers shall be able to delete their own products                                         | High     | Implemented |
| FR-PM-04 | System shall support multiple images per product with primary selection                    | Medium   | Implemented |
| FR-PM-05 | Products shall have status states: Draft, Active, Blocked                                  | High     | Implemented |
| FR-PM-06 | System shall allow bulk product creation                                                   | Medium   | Implemented |
| FR-PM-07 | Users shall be able to view all active products                                            | High     | Implemented |
| FR-PM-08 | Users shall be able to search products by title and description                            | High     | Implemented |
| FR-PM-09 | Users shall be able to filter products by category                                         | High     | Implemented |
| FR-PM-10 | Users shall be able to filter products by price range                                      | Medium   | Implemented |
| FR-PM-11 | System shall display product details including images, price, stock, and seller info       | High     | Implemented |

### 5.3 Category Management (FR-CM)

| ID       | Requirement                                                 | Priority | Status      |
| -------- | ----------------------------------------------------------- | -------- | ----------- |
| FR-CM-01 | Admins shall be able to create categories                   | High     | Implemented |
| FR-CM-02 | Admins shall be able to update categories                   | High     | Implemented |
| FR-CM-03 | Admins shall be able to delete categories                   | High     | Implemented |
| FR-CM-04 | System shall support hierarchical categories (parent-child) | Medium   | Implemented |
| FR-CM-05 | Users shall be able to view all categories                  | High     | Implemented |
| FR-CM-06 | Categories shall have active/inactive status                | Medium   | Implemented |
| FR-CM-07 | Categories shall support SEO-friendly slugs                 | Low      | Implemented |

### 5.4 Shopping Cart (FR-SC)

| ID       | Requirement                                            | Priority | Status      |
| -------- | ------------------------------------------------------ | -------- | ----------- |
| FR-SC-01 | Users shall be able to add products to cart            | High     | Implemented |
| FR-SC-02 | Users shall be able to update product quantity in cart | High     | Implemented |
| FR-SC-03 | Users shall be able to remove products from cart       | High     | Implemented |
| FR-SC-04 | Users shall be able to clear entire cart               | Medium   | Implemented |
| FR-SC-05 | Cart shall persist across page refreshes               | High     | Implemented |
| FR-SC-06 | Cart shall display subtotal and item count             | High     | Implemented |
| FR-SC-07 | Cart shall validate stock availability                 | Medium   | Implemented |
| FR-SC-08 | Cart icon shall display current item count badge       | Medium   | Implemented |

### 5.5 Order Management (FR-OM)

| ID       | Requirement                                                                         | Priority | Status      |
| -------- | ----------------------------------------------------------------------------------- | -------- | ----------- |
| FR-OM-01 | Customers shall be able to place orders                                             | High     | Implemented |
| FR-OM-02 | System shall capture shipping address during checkout                               | High     | Implemented |
| FR-OM-03 | System shall calculate order total with tax                                         | High     | Implemented |
| FR-OM-04 | Customers shall receive order confirmation                                          | High     | Implemented |
| FR-OM-05 | Sellers shall be able to view their incoming orders                                 | High     | Implemented |
| FR-OM-06 | Sellers shall be able to update order status                                        | High     | Implemented |
| FR-OM-07 | Customers shall be able to view their order history                                 | Medium   | Implemented |
| FR-OM-08 | Orders shall have status states: Pending, Processing, Shipped, Delivered, Cancelled | High     | Implemented |

### 5.6 External API (FR-EA)

| ID       | Requirement                                                                  | Priority | Status      |
| -------- | ---------------------------------------------------------------------------- | -------- | ----------- |
| FR-EA-01 | Sellers shall be able to generate API keys                                   | High     | Implemented |
| FR-EA-02 | Sellers shall be able to view their API keys                                 | High     | Implemented |
| FR-EA-03 | Sellers shall be able to activate/deactivate API keys                        | Medium   | Implemented |
| FR-EA-04 | Sellers shall be able to delete API keys                                     | Medium   | Implemented |
| FR-EA-05 | External clients shall be able to manage products via API key authentication | High     | Implemented |
| FR-EA-06 | API keys shall support expiration dates                                      | Medium   | Implemented |
| FR-EA-07 | System shall track API key usage timestamps                                  | Low      | Implemented |

---

## 6. Non-Functional Requirements

### 6.1 Performance (NFR-PF)

| ID        | Requirement                                 | Target        | Implementation                          |
| --------- | ------------------------------------------- | ------------- | --------------------------------------- |
| NFR-PF-01 | Page load time shall be under 3 seconds     | < 3s          | Vite build optimization, code splitting |
| NFR-PF-02 | API response time shall be under 500ms      | < 500ms       | Async/await, database indexes           |
| NFR-PF-03 | System shall handle 500 concurrent requests | 500 req/min   | Rate limiting, connection pooling       |
| NFR-PF-04 | Database queries shall use pagination       | 20 items/page | Skip/Take with EF Core                  |
| NFR-PF-05 | Frontend shall implement lazy loading       | On scroll     | React.lazy, dynamic imports             |
| NFR-PF-06 | Images shall be optimized for web           | < 200KB       | Compression, responsive images          |

### 6.2 Security (NFR-SC)

| ID        | Requirement                                       | Implementation                           |
| --------- | ------------------------------------------------- | ---------------------------------------- |
| NFR-SC-01 | Passwords shall be hashed using secure algorithms | ASP.NET Identity (PBKDF2)                |
| NFR-SC-02 | Authentication shall use JWT tokens               | HS256 symmetric signing                  |
| NFR-SC-03 | API shall implement CORS protection               | Origin whitelist                         |
| NFR-SC-04 | System shall prevent SQL injection                | Entity Framework parameterized queries   |
| NFR-SC-05 | System shall prevent XSS attacks                  | React DOM escaping, input sanitization   |
| NFR-SC-06 | Sensitive endpoints shall require authentication  | [Authorize] attribute                    |
| NFR-SC-07 | Rate limiting shall prevent brute force attacks   | 7 login attempts / 5 minutes             |
| NFR-SC-08 | API keys shall be cryptographically secure        | 32-byte random generation                |
| NFR-SC-09 | Passwords shall meet complexity requirements      | Min 6 chars, uppercase, lowercase, digit |

### 6.3 Reliability (NFR-RL)

| ID        | Requirement                                         | Implementation              |
| --------- | --------------------------------------------------- | --------------------------- |
| NFR-RL-01 | System shall handle errors gracefully               | Global exception middleware |
| NFR-RL-02 | Frontend shall display user-friendly error messages | ErrorBoundary component     |
| NFR-RL-03 | API shall return consistent error formats           | RFC 7807 Problem Details    |
| NFR-RL-04 | System shall log errors for debugging               | Structured logging          |
| NFR-RL-05 | Database transactions shall be atomic               | EF Core SaveChangesAsync    |

### 6.4 Usability (NFR-US)

| ID        | Requirement                              | Implementation           |
| --------- | ---------------------------------------- | ------------------------ |
| NFR-US-01 | UI shall be responsive across devices    | Tailwind CSS breakpoints |
| NFR-US-02 | UI shall provide loading indicators      | Spinner components       |
| NFR-US-03 | UI shall provide toast notifications     | Sonner library           |
| NFR-US-04 | Forms shall provide validation feedback  | React Hook Form + Zod    |
| NFR-US-05 | Navigation shall be intuitive            | Clear menu structure     |
| NFR-US-06 | System shall support keyboard navigation | ARIA attributes          |

### 6.5 Scalability (NFR-SL)

| ID        | Requirement                                   | Implementation            |
| --------- | --------------------------------------------- | ------------------------- |
| NFR-SL-01 | Architecture shall support horizontal scaling | Stateless API design      |
| NFR-SL-02 | Database shall use connection pooling         | Npgsql connection pool    |
| NFR-SL-03 | Code shall be modular and maintainable        | Layered architecture      |
| NFR-SL-04 | System shall use dependency injection         | ASP.NET Core DI container |

### 6.6 Maintainability (NFR-MT)

| ID        | Requirement                               | Implementation         |
| --------- | ----------------------------------------- | ---------------------- |
| NFR-MT-01 | Code shall follow SOLID principles        | Repository pattern, DI |
| NFR-MT-02 | TypeScript shall be used for type safety  | Strict mode enabled    |
| NFR-MT-03 | API documentation shall be auto-generated | Swagger/OpenAPI        |
| NFR-MT-04 | Configuration shall be externalized       | Environment variables  |
| NFR-MT-05 | Database changes shall be versioned       | EF Core migrations     |

### 6.7 Compatibility (NFR-CP)

| ID        | Requirement                            | Implementation          |
| --------- | -------------------------------------- | ----------------------- |
| NFR-CP-01 | Frontend shall support modern browsers | ES2020+ with polyfills  |
| NFR-CP-02 | API shall follow REST conventions      | RESTful endpoint design |
| NFR-CP-03 | Data shall use JSON format             | application/json        |
| NFR-CP-04 | Dates shall use ISO 8601 format        | UTC timestamps          |

---

## 7. Database Design

### 7.1 Entity Relationship Diagram

```
+---------------+       +---------------+       +---------------+
|    AppUser    |       |    AppRole    |       |  AppUserRole  |
+---------------+       +---------------+       +---------------+
| Id (PK)       |<------| Id (PK)       |<------| UserId (FK)   |
| Email         |       | Name          |       | RoleId (FK)   |
| FirstName     |       | Description   |       +---------------+
| LastName      |       +---------------+
| IsActive      |
| CreatedAt     |
+-------+-------+
        |
        | 1:1
        v
+---------------+       +---------------+       +------------------+
| SellerProfile |       |  SellerApiKey |       | MarketplaceCred  |
+---------------+       +---------------+       +------------------+
| Id (PK)       |<--+---| SellerId (FK) |       | SellerId (FK)    |
| UserId (FK)   |   |   | ApiKey (Uniq) |       | Platform         |
| StoreName     |   |   | Name          |       | ApiKey           |
| LogoUrl       |   |   | IsActive      |       | Secret           |
| IsApproved    |   |   | ExpiresAt     |       +------------------+
| CreatedAt     |   |   | LastUsedAt    |
+-------+-------+   |   +---------------+
        |           |
        | 1:M       |
        v           |
+---------------+   |   +---------------+
|    Product    |---+   |  ProductImage |
+---------------+       +---------------+
| Id (PK)       |<------| ProductId (FK)|
| SellerId (FK) |       | ImageUrl      |
| CategoryId(FK)|       | IsPrimary     |
| Title         |       | DisplayOrder  |
| Description   |       +---------------+
| Price         |
| Stock         |       +---------------+
| Status        |       |     Order     |
| CreatedAt     |<------+---------------+
+-------+-------+       | Id (PK)       |
        |               | ProductId(FK) |
        |               | CustomerId(FK)|
+-------v-------+       | Quantity      |
|   Category    |       | Status        |
+---------------+       | OrderDate     |
| Id (PK)       |       | Notes         |
| ParentId (FK) |<--+   +---------------+
| CategoryName  |   |
| SeoSlug       |---+ (Self-referencing)
| IsActive      |
| CreatedAt     |
+---------------+
```

### 7.2 Database Indexes

| Table         | Index Name                 | Columns         | Purpose                    |
| ------------- | -------------------------- | --------------- | -------------------------- |
| Products      | IX_Products_SellerId       | SellerId        | Fast seller product lookup |
| Products      | IX_Products_CategoryId     | CategoryId      | Fast category filtering    |
| Orders        | IX_Orders_ProductId        | ProductId       | Order-product relationship |
| Orders        | IX_Orders_CustomerId       | CustomerId      | Customer order history     |
| SellerApiKeys | IX_SellerApiKeys_ApiKey    | ApiKey (Unique) | API key validation         |
| Categories    | IX_Categories_ParentId     | ParentId        | Hierarchy navigation       |
| ProductImages | IX_ProductImages_ProductId | ProductId       | Image retrieval            |

---

## 8. API Documentation

### 8.1 Authentication Endpoints

```
POST /api/v1/auth/register
  Request: { email, password, firstName, lastName }
  Response: { isAuthenticated, token, email, firstName, lastName, role, message }

POST /api/v1/auth/register-seller
  Request: { email, password, firstName, lastName, storeName, logoUrl }
  Response: { isAuthenticated, token, email, firstName, lastName, role, message }

POST /api/v1/auth/login
  Request: { email, password }
  Response: { isAuthenticated, token, email, firstName, lastName, role, message }

GET /api/v1/auth/test-token
  Headers: Authorization: Bearer <token>
  Response: { valid, userId, email, role }
```

### 8.2 Product Endpoints

```
GET    /api/v1/product              [Admin]     -> Get all products
GET    /api/v1/product/active                   -> Get active products
GET    /api/v1/product/{id}                     -> Get product by ID
GET    /api/v1/product/category/{categoryId}    -> Get by category
GET    /api/v1/product/search?searchTerm=       -> Search products
GET    /api/v1/product/price-range?min=&max=    -> Filter by price
GET    /api/v1/product/my-products  [Seller]    -> Seller's products
POST   /api/v1/product              [Seller]    -> Create product
PUT    /api/v1/product/{id}         [Seller]    -> Update product
DELETE /api/v1/product/{id}         [Seller]    -> Delete product
```

### 8.3 Category Endpoints

```
GET    /api/v1/category                         -> Get all categories
GET    /api/v1/category/hierarchical            -> Get category tree
GET    /api/v1/category/root                    -> Get root categories
GET    /api/v1/category/{id}                    -> Get category by ID
POST   /api/v1/category             [Admin]     -> Create category
PUT    /api/v1/category/{id}        [Admin]     -> Update category
DELETE /api/v1/category/{id}        [Admin]     -> Delete category
```

### 8.4 Order Endpoints

```
GET    /api/v1/order                [Admin]     -> Get all orders
GET    /api/v1/order/{id}           [Auth]      -> Get order by ID
GET    /api/v1/order/my-orders      [Seller]    -> Seller's incoming orders
POST   /api/v1/order                [Auth]      -> Create order
PUT    /api/v1/order/{id}           [Auth]      -> Update order status
DELETE /api/v1/order/{id}           [Auth]      -> Delete order
```

### 8.5 External API (API Key Auth)

```
Headers Required: X-API-Key: mec_[key]

GET    /api/v1/external/products                -> Get seller's products
GET    /api/v1/external/products/{id}           -> Get specific product
POST   /api/v1/external/products                -> Create product
PUT    /api/v1/external/products/{id}           -> Update product
DELETE /api/v1/external/products/{id}           -> Delete product
```

---

## 9. Security Implementation

### 9.1 Authentication Flow

```
+----------+     1. Login Request      +----------+
|  Client  | ------------------------> |   API    |
|          |   {email, password}       |  Server  |
+----------+                           +----+-----+
                                            |
                                            v
                                    +-------+-------+
                                    |   Validate    |
                                    |  Credentials  |
                                    +-------+-------+
                                            |
                                            v
+----------+     2. JWT Token          +----------+
|  Client  | <------------------------ |   API    |
|          |   {token, user data}      |  Server  |
+----+-----+                           +----------+
     |
     | 3. Store token in localStorage
     v
+----------+     4. Protected Request  +----------+
|  Client  | ------------------------> |   API    |
|          |   Authorization: Bearer   |  Server  |
+----------+                           +----+-----+
                                            |
                                            v
                                    +-------+-------+
                                    |   Validate    |
                                    |     JWT       |
                                    +-------+-------+
                                            |
                                            v
+----------+     5. Response           +----------+
|  Client  | <------------------------ |   API    |
|          |   Protected data          |  Server  |
+----------+                           +----------+
```

### 9.2 Security Measures Summary

| Threat                  | Mitigation                                  |
| ----------------------- | ------------------------------------------- |
| **SQL Injection**       | Entity Framework parameterized queries      |
| **XSS**                 | React DOM escaping, Content Security Policy |
| **CSRF**                | JWT tokens (no cookies for auth)            |
| **Brute Force**         | Rate limiting (7 attempts/5 min)            |
| **Man-in-the-Middle**   | HTTPS enforcement                           |
| **Token Theft**         | Short expiration, secure storage            |
| **Unauthorized Access** | Role-based authorization                    |
| **API Abuse**           | API key authentication, rate limiting       |

---

## 10. Design Patterns Used

### 10.1 Backend Patterns

| Pattern                   | Implementation                              | Purpose                      |
| ------------------------- | ------------------------------------------- | ---------------------------- |
| **Repository Pattern**    | `GenericRepository<T>`, `ProductRepository` | Abstract data access         |
| **Service Layer Pattern** | `IProductService` -> `ProductManager`       | Encapsulate business logic   |
| **Dependency Injection**  | ASP.NET Core DI Container                   | Loose coupling               |
| **DTO Pattern**           | `ProductDto`, `CreateProductDto`            | Data transfer between layers |
| **Factory Pattern**       | `AppDbContextFactory`                       | Database context creation    |
| **Middleware Pattern**    | Exception handling, API key auth            | Request pipeline processing  |
| **Options Pattern**       | `JwtSettings`, `AdminUserSettings`          | Configuration management     |
| **Strategy Pattern**      | JWT vs API Key authentication               | Multiple auth strategies     |

### 10.2 Frontend Patterns

| Pattern                    | Implementation                             | Purpose                 |
| -------------------------- | ------------------------------------------ | ----------------------- |
| **Context Provider**       | `AuthContext`, `CartContext`               | Global state management |
| **Custom Hooks**           | `useAuth()`, `useCart()`, `useApi()`       | Reusable logic          |
| **Container/Presenter**    | Pages (container) + Components (presenter) | Separation of concerns  |
| **Higher-Order Component** | `ProtectedRoute`                           | Cross-cutting concerns  |
| **Compound Components**    | shadcn/ui components                       | Flexible composition    |
| **Render Props**           | Error boundaries                           | Flexible rendering      |

### 10.3 Architectural Patterns

| Pattern                 | Implementation                    | Purpose                 |
| ----------------------- | --------------------------------- | ----------------------- |
| **N-Tier Architecture** | WebAPI, Business, DAL, DTO layers | Separation of concerns  |
| **RESTful API**         | HTTP methods, resource-based URLs | Standardized API design |
| **Client-Server**       | React frontend + .NET backend     | Separation of concerns  |
| **MVC (Backend)**       | Controllers, Services, Models     | Request handling        |

---

## Appendix A: Environment Configuration

### Backend (.env)

```env
# Database
DB_HOST=localhost
DB_PORT=5432
DB_NAME=MockECommerce-DB
DB_USERNAME=postgres
DB_PASSWORD=your_password

# JWT
JWT_ISSUER=MockECommerceServer
JWT_AUDIENCE=MockECommerceClient
JWT_SECRET_KEY=your_256_bit_secret_key
JWT_EXPIRY_MINUTES=60

# Admin User
ADMIN_FULLNAME=Administrator
ADMIN_EMAIL=admin@mockecommerce.com
ADMIN_PASSWORD=Admin123!
```

### Frontend (.env)

```env
VITE_API_BASE_URL=http://localhost:5000/api/v1/
VITE_APP_NAME=Market Hub
VITE_ENABLE_DEV_TOOLS=true
```

---

## Appendix B: Running the Application

### Backend

```bash
cd BTK-MockECommerce-Server
dotnet restore
dotnet ef database update --project MockECommerce.DAL --startup-project MockECommerce.WebAPI
dotnet run --project MockECommerce.WebAPI
```

### Frontend

```bash
cd BTK-MockECommerce-Front
pnpm install
pnpm dev
```

### Access Points

- **Frontend:** http://localhost:5173
- **Backend API:** http://localhost:5000
- **Swagger UI:** http://localhost:5000/swagger

---

## Appendix C: Default Test Users

| Role     | Email                   | Password     |
| -------- | ----------------------- | ------------ |
| Admin    | admin@mockecommerce.com | Admin123!    |
| Seller   | seller@test.com         | Seller123!   |
| Customer | customer@test.com       | Customer123! |

---

_Document Version: 1.0_
_Last Updated: December 2025_
_Course: Object-Oriented Programming_
