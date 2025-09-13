# 📚 E-Library Backend API

A modern, scalable .NET 9.0 Web API backend for the E-Library application with comprehensive book management, user authentication, and borrowing system. Built with Dapper ORM, PostgreSQL database, and Supabase integration.

[![.NET](https://img.shields.io/badge/.NET-9.0-blue.svg)](https://dotnet.microsoft.com/download)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15+-blue.svg)](https://www.postgresql.org/)
[![Dapper](https://img.shields.io/badge/Dapper-2.1.66-green.svg)](https://github.com/DapperLib/Dapper)
[![License](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## 🌟 Features

### 🔐 Authentication & Security
- **JWT-based Authentication** - Secure token-based authentication
- **Password Hashing** - BCrypt for secure password storage
- **Role-based Authorization** - Admin and user role management
- **CORS Configuration** - Secure cross-origin resource sharing

### 📖 Book Management
- **CRUD Operations** - Complete book management system
- **Search & Filter** - Advanced book search capabilities
- **ISBN Validation** - Unique book identification
- **Cover Image Support** - Book cover image handling

### 📚 Borrowing System
- **Book Borrowing** - Date-based borrowing with pricing
- **Return Management** - Automated return tracking
- **ID Card Upload** - User verification system
- **Borrowing History** - Complete transaction records

### 🗄️ Database & Storage
- **PostgreSQL Integration** - Robust database support
- **Supabase Storage** - Cloud file storage for images
- **Dapper ORM** - High-performance data access
- **Database Migration** - Automatic schema initialization

### 🚀 Deployment & DevOps
- **Docker Support** - Containerized deployment
- **Render Integration** - One-click cloud deployment
- **CI/CD Pipeline** - Automated testing and deployment
- **Health Monitoring** - Service health checks

## 🛠️ Tech Stack

| Technology | Version | Purpose |
|------------|---------|---------|
| **.NET** | 9.0 | Web API framework |
| **Dapper** | 2.1.66 | Micro ORM for database operations |
| **PostgreSQL** | 15+ | Primary database |
| **Supabase** | 1.0.0 | Cloud database and storage |
| **JWT Bearer** | 9.0.9 | Authentication tokens |
| **BCrypt** | 4.0.3 | Password hashing |
| **Swagger** | 7.0.0 | API documentation |
| **Docker** | Latest | Containerization |

## 📋 Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/) or [Supabase](https://supabase.com)
- [Docker](https://www.docker.com/get-started) (optional)
- [Git](https://git-scm.com/downloads)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/AbbasSk2004/Libro-E-library-Backend.git
cd Libro-E-library-Backend
```

### 2. Environment Setup

```bash
# Copy environment template
cp .env.example .env

# Edit .env with your actual values
nano .env  # or use your preferred editor
```

### 3. Install Dependencies

```bash
dotnet restore
```

### 4. Run the Application

```bash
dotnet run
```

The API will be available at:
- **HTTPS**: `https://localhost:7001`
- **HTTP**: `http://localhost:5000`
- **Swagger UI**: `https://localhost:7001/swagger`
- **Health Check**: `https://localhost:7001/health`

## 📚 API Documentation

### 🔐 Authentication Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/api/auth/register` | User registration | ❌ |
| `POST` | `/api/auth/login` | User login | ❌ |
| `POST` | `/api/auth/logout` | User logout | ✅ |
| `GET` | `/api/auth/profile` | Get user profile | ✅ |

### 📖 Book Management Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/api/books` | Get all books (with search) | ❌ |
| `GET` | `/api/books/{id}` | Get book by ID | ❌ |
| `POST` | `/api/books` | Create new book | ✅ (Admin) |
| `PUT` | `/api/books/{id}` | Update book | ✅ (Admin) |
| `DELETE` | `/api/books/{id}` | Delete book | ✅ (Admin) |

### 📚 Borrowing Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `POST` | `/api/books/{id}/borrow` | Borrow a book | ✅ |
| `POST` | `/api/books/{id}/return` | Return a book | ✅ |
| `GET` | `/api/books/borrowed-books` | Get user's borrowed books | ✅ |
| `GET` | `/api/books/borrowing-history` | Get borrowing history | ✅ |

### 🏥 System Endpoints

| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| `GET` | `/health` | Health check | ❌ |
| `GET` | `/swagger` | API documentation | ❌ |

## 🔧 Configuration

### Environment Variables

Create a `.env` file based on `.env.example`:

```bash
# Database Configuration
DATABASE_CONNECTION_STRING=Host=your-supabase-host.supabase.co;Port=5432;Database=postgres;Username=postgres.your-project-ref;Password=your-password;SSL Mode=Require;Trust Server Certificate=true

# Supabase Configuration
SUPABASE_URL=https://your-project-ref.supabase.co
SUPABASE_ANON_KEY=your-supabase-anon-key
SUPABASE_SERVICE_ROLE_KEY=your-supabase-service-role-key

# JWT Configuration
JWT_KEY=your-super-secret-jwt-key-that-is-at-least-32-characters-long
JWT_ISSUER=E-Library-API
JWT_AUDIENCE=E-Library-Client

# Render Configuration (automatically set)
PORT=10000
```

### CORS Configuration

The API is configured to allow requests from:
- **Development**: `http://localhost:5173` (Vite dev server)
- **Production**: `https://libro-e-library.vercel.app` (Vercel frontend)

## 📁 Project Structure

```
E-Library.API/
├── 📁 Controllers/              # API Controllers
│   ├── AuthController.cs       # Authentication endpoints
│   ├── BooksController.cs      # Book management endpoints
│   └── AdminController.cs      # Admin-only endpoints
├── 📁 Data/                    # Data Access Layer
│   ├── DatabaseConnection.cs   # Database connection management
│   ├── UserRepository.cs       # User data operations
│   ├── BookRepository.cs       # Book data operations
│   ├── BorrowedBookRepository.cs # Borrowing data operations
│   └── ReturnedBookRepository.cs # Return data operations
├── 📁 DTOs/                    # Data Transfer Objects
│   ├── AuthDTOs.cs            # Authentication DTOs
│   └── BookDTOs.cs            # Book-related DTOs
├── 📁 Models/                  # Database Models
│   ├── User.cs                # User entity
│   ├── Book.cs                # Book entity
│   ├── BorrowedBook.cs        # Borrowed book entity
│   └── ReturnedBook.cs        # Returned book entity
├── 📁 Services/                # Business Logic Layer
│   ├── AuthService.cs         # Authentication logic
│   ├── BookService.cs         # Book management logic
│   └── StorageService.cs      # File storage logic
├── 📁 .github/workflows/       # CI/CD Pipelines
│   ├── ci-cd.yml              # Main CI/CD pipeline
│   └── docker-build.yml       # Docker build pipeline
├── 📄 Program.cs               # Application entry point
├── 📄 appsettings.json         # Development configuration
├── 📄 appsettings.Production.json # Production configuration
├── 📄 Dockerfile               # Docker container configuration
├── 📄 docker-compose.yml       # Docker Compose configuration
├── 📄 render.yaml              # Render deployment configuration
└── 📄 .env.example             # Environment variables template
```

## 🗄️ Database Schema

### Users Table
```sql
CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    Email VARCHAR(255) UNIQUE NOT NULL,
    Name VARCHAR(255) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(50) DEFAULT 'User',
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Books Table
```sql
CREATE TABLE Books (
    Id SERIAL PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Author VARCHAR(255) NOT NULL,
    Isbn VARCHAR(20) UNIQUE NOT NULL,
    Description TEXT,
    PublishedYear INTEGER,
    Available BOOLEAN DEFAULT TRUE,
    CoverImage VARCHAR(500),
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### BorrowedBooks Table
```sql
CREATE TABLE BorrowedBooks (
    Id SERIAL PRIMARY KEY,
    UserId INTEGER REFERENCES Users(Id),
    BookId INTEGER REFERENCES Books(Id),
    BorrowedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    DueDate TIMESTAMP NOT NULL,
    ReturnedAt TIMESTAMP NULL,
    Price DECIMAL(10,2) NOT NULL,
    IdCardImagePath VARCHAR(500),
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

## 🚀 Deployment

### 🌐 Render Deployment (Recommended)

#### Option 1: Using Render Dashboard
1. **Connect Repository**:
   - Go to [Render Dashboard](https://dashboard.render.com)
   - Click "New +" → "Web Service"
   - Connect your GitHub account
   - Select `AbbasSk2004/Libro-E-library-Backend`

2. **Configure Service**:
   ```
   Name: libro-e-library-backend
   Environment: Dotnet
   Build Command: dotnet publish -c Release -o ./publish
   Start Command: dotnet ./publish/E-Library.API.dll
   ```

3. **Set Environment Variables**:
   ```
   ASPNETCORE_ENVIRONMENT=Production
   ASPNETCORE_URLS=http://0.0.0.0:$PORT
   DATABASE_CONNECTION_STRING=your-supabase-connection-string
   JWT_KEY=your-32-character-secret-key
   JWT_ISSUER=E-Library-API
   JWT_AUDIENCE=E-Library-Client
   SUPABASE_URL=your-supabase-url
   SUPABASE_ANON_KEY=your-supabase-anon-key
   SUPABASE_SERVICE_ROLE_KEY=your-supabase-service-role-key
   ```

#### Option 2: Using render.yaml (Infrastructure as Code)
The repository includes a `render.yaml` file for automatic deployment:
1. Connect your GitHub repository to Render
2. Render will automatically detect and use the configuration
3. Follow the prompts to create the service

### 🐳 Docker Deployment

#### Local Development
```bash
# Build Docker image
docker build -t libro-e-library-backend .

# Run container
docker run -p 8080:80 \
  -e DATABASE_CONNECTION_STRING="your-connection-string" \
  -e JWT_KEY="your-jwt-key" \
  -e SUPABASE_URL="your-supabase-url" \
  -e SUPABASE_ANON_KEY="your-anon-key" \
  -e SUPABASE_SERVICE_ROLE_KEY="your-service-role-key" \
  libro-e-library-backend
```

#### Docker Compose
```bash
# Copy environment file
cp .env.example .env
# Edit .env with your values

# Start services
docker-compose up -d
```

### 🔄 CI/CD Pipeline

The repository includes GitHub Actions workflows for:
- **Automated Testing**: Runs on every push and pull request
- **Docker Build**: Builds and tests Docker images
- **Deployment**: Automatic deployment to Render on main branch

## 🔐 Security Features

### Authentication
- **JWT Tokens**: Secure, stateless authentication
- **Password Hashing**: BCrypt with salt for password security
- **Token Expiration**: Configurable token lifetime
- **Role-based Access**: Admin and user role separation

### Data Protection
- **Environment Variables**: Sensitive data stored securely
- **CORS Configuration**: Restricted cross-origin access
- **Input Validation**: Comprehensive request validation
- **SQL Injection Prevention**: Parameterized queries with Dapper

### File Security
- **File Type Validation**: Restricted upload types
- **File Size Limits**: Configurable upload size limits
- **Secure Storage**: Files stored in Supabase with proper access controls

## 🏥 Monitoring & Health Checks

### Health Check Endpoint
- **URL**: `GET /health`
- **Response**: Service status and uptime
- **Use Case**: Load balancer health checks, monitoring

### API Documentation
- **Swagger UI**: Available at `/swagger`
- **OpenAPI Spec**: Available at `/swagger/v1/swagger.json`
- **Interactive Testing**: Test endpoints directly from browser

## 🔗 Frontend Integration

### Development
- **Local API**: `https://localhost:7001/api`
- **CORS**: Configured for `http://localhost:5173`

### Production
- **Production API**: `https://your-render-app.onrender.com/api`
- **CORS**: Configured for `https://libro-e-library.vercel.app`

### API Usage Example
```javascript
// Login
const response = await fetch('https://your-api.com/api/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
  },
  body: JSON.stringify({
    email: 'user@example.com',
    password: 'password123'
  })
});

// Use token for authenticated requests
const token = response.data.token;
const booksResponse = await fetch('https://your-api.com/api/books', {
  headers: {
    'Authorization': `Bearer ${token}`
  }
});
```

## 🧪 Testing

### Run Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test Tests/E-Library.API.Tests/
```

### Test Coverage
- Unit tests for all services
- Integration tests for API endpoints
- Database connection tests
- Authentication flow tests

## 📝 Development Guidelines

### Code Style
- Follow C# naming conventions
- Use async/await for I/O operations
- Implement proper error handling
- Add XML documentation for public methods

### Git Workflow
1. Create feature branch from `main`
2. Make changes and commit with descriptive messages
3. Push branch and create pull request
4. Review and merge after approval

### Environment Setup
1. Use `.env.example` as template
2. Never commit `.env` files
3. Use different databases for dev/staging/prod
4. Rotate secrets regularly

## 🐛 Troubleshooting

### Common Issues

#### Database Connection Failed
```
Error: Connection refused
Solution: 
1. Check DATABASE_CONNECTION_STRING
2. Verify Supabase credentials
3. Ensure network access is enabled
```

#### JWT Authentication Failed
```
Error: Invalid token
Solution:
1. Check JWT_KEY is 32+ characters
2. Verify JWT_ISSUER and JWT_AUDIENCE
3. Ensure token is not expired
```

#### CORS Issues
```
Error: CORS policy blocked
Solution:
1. Check frontend URL in CORS configuration
2. Verify credentials are included in requests
3. Ensure preflight requests are handled
```

### Debug Mode
```bash
# Enable detailed logging
export ASPNETCORE_ENVIRONMENT=Development
dotnet run --verbosity detailed
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 👥 Authors

- **AbbasSk2004** - *Initial work* - [GitHub](https://github.com/AbbasSk2004)

## 🙏 Acknowledgments

- [.NET](https://dotnet.microsoft.com/) - Web API framework
- [Dapper](https://github.com/DapperLib/Dapper) - Micro ORM
- [Supabase](https://supabase.com/) - Backend as a Service
- [Render](https://render.com/) - Cloud hosting platform
- [Docker](https://www.docker.com/) - Containerization platform

## 📞 Support

For support and questions:
- Create an [Issue](https://github.com/AbbasSk2004/Libro-E-library-Backend/issues)
- Check the [Documentation](https://github.com/AbbasSk2004/Libro-E-library-Backend/wiki)
- Review [API Documentation](https://your-api-url.com/swagger)

---

⭐ **Star this repository if you found it helpful!**