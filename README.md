# AI-Powered Quiz Application

A comprehensive microservice built with C# .NET 9 and PostgreSQL that provides AI-powered quiz generation, evaluation, and score tracking capabilities.

## 🚀 Features

### Core Functionalities (Mandatory)

#### Authentication
- Mock authentication service accepting any username/password
- Returns signed JSON Web Token (JWT) for subsequent requests
- Token validation for all quiz endpoints

#### Quiz Management REST API
- **Generate Quiz**: AI-powered quiz generation with adaptive difficulty
- **Submit Quiz**: Answer submission with AI-based evaluation and scoring
- **Quiz History**: Retrieve quiz history with advanced filtering
- **Retry Quiz**: Retry quizzes with adaptive difficulty adjustment
- **Hint Generation**: AI provides contextual hints for questions

#### AI Features
- **Hint Generation**: AI provides helpful hints when requested
- **Result Suggestions**: AI suggests 2 improvement tips based on mistakes
- **Adaptive Question Difficulty**: Adjusts question difficulty based on past performance
- **Smart Content Generation**: Uses Groq API for intelligent quiz content

### Bonus Features ✨

- **Leaderboard API**: Display top scores by grade/subject
- **Real-time Performance Tracking**: Advanced analytics and scoring
- **Interactive Swagger UI**: Complete API documentation with authentication support

## 🏗️ Architecture

```
QuizApp/
├── QuizApp.API/                 # Web API Layer
│   ├── Controllers/            # API Controllers
│   ├── Program.cs             # Application configuration
│   └── appsettings.json       # Configuration settings
├── QuizApp.Core/               # Domain Layer
│   ├── Models/                # Domain entities
│   ├── DTOs/                  # Data Transfer Objects
│   └── Interfaces/            # Service interfaces
├── QuizApp.Infrastructure/     # Infrastructure Layer
│   ├── Data/                  # Database context
│   └── Services/   
    └── Migrations/   # Service implementations
└── README.md                  # This file
└──QuizApp_Postman_Collection.json
```

## 🔧 Technology Stack

- **Backend**: .NET 9, ASP.NET Core Web API
- **Database**: PostgreSQL 15
- **AI Integration**: Groq API (Llama3-8b-8192 model)
- **Authentication**: JWT Bearer tokens
- **Documentation**: Swagger/OpenAPI with interactive testing

## 🚀 Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL](https://www.postgresql.org/download/)

### Local Development Setup

1. **Setup PostgreSQL**
   ```sql
   CREATE DATABASE quiz_app_db;
   CREATE USER postgres WITH PASSWORD 'password';
   GRANT ALL PRIVILEGES ON DATABASE quiz_app_db TO postgres;
   ```

2. **Run your database migrations**
   ```bash
   # Use your existing migration setup
   dotnet ef database update
   ```

3. **Configure appsettings.json**
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=quiz_app_db;Username=postgres;Password=password"
     },
     "GroqApiKey": "gsk-your-actual-groq-api-key-here",
     "JwtSettings": {
       "SecretKey": "your-super-secret-key-that-is-at-least-32-characters-long-for-jwt-signing"
     }
   }
   ```

4. **Run the application**
   ```bash
   cd QuizApp.API
   dotnet run
   ```
   
   **🎯 Swagger UI Access:**
   - **Automatic**: Opens when running from Visual Studio
   - **Manual**: Navigate to `http://localhost:5000` or `https://localhost:5001`
   - **Interactive Testing**: Use "Authorize" button in Swagger for JWT authentication

## 🔐 JWT Authentication Guide

### Getting a Token
1. Use the `/api/auth/login` endpoint with any username/password
2. Copy the returned JWT token
3. In Swagger UI, click "Authorize" button
4. Enter: `Bearer YOUR_TOKEN_HERE` (note: include "Bearer " prefix)
5. Click "Authorize" to apply to all endpoints

### Troubleshooting JWT Issues
- Make sure the JWT secret key matches in both `Program.cs` and `AuthService.cs`
- Include "Bearer " prefix when pasting token in Swagger
- Check token expiration (24-hour default)
- Ensure database connection is working for user creation

## 🔧 Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | See appsettings.json |
| `JwtSettings__SecretKey` | JWT signing secret | Default key provided |
| `GroqApiKey` | Groq API key for AI features | Required for AI features |

### AI Integration Details

The application uses **Groq API** with the **Llama3-8b-8192** model for:

- **Quiz Generation**: Creates contextual questions based on subject and grade level
- **Hint Generation**: Provides helpful hints without revealing answers
- **Improvement Suggestions**: Analyzes incorrect answers to suggest study areas
- **Adaptive Difficulty**: Adjusts question complexity based on user performance

**API Endpoints Used**:
- `POST https://api.groq.com/openai/v1/chat/completions`

**Models Used**:
- `llama3-8b-8192` (primary model for all AI features)

## 📚 API Documentation

### Authentication Endpoints

#### POST `/api/auth/login`
Login with any username/password (mock authentication).

**Request Body:**
```json
{
  "username": "testuser",
  "password": "test123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "username": "testuser",
  "userId": 1,
  "expiresAt": "2024-08-25T12:47:14Z"
}
```

#### GET `/api/auth/me`
Verify current authentication status (requires Bearer token).

### Quiz Management Endpoints

#### POST `/api/quiz/generate`
Generate a new AI-powered quiz.

**Request Body:**
```json
{
  "subject": "Mathematics",
  "gradeLevel": "Grade 5",
  "numberOfQuestions": 5,
  "specificTopics": "Addition and Subtraction"
}
```

#### POST `/api/quiz/submit`
Submit quiz answers for evaluation.

**Request Body:**
```json
{
  "quizId": 1,
  "answers": [
    {
      "questionId": 1,
      "selectedAnswerId": 1
    },
    {
      "questionId": 2,
      "selectedAnswerId": 5
    }
  ]
}
```

#### POST `/api/quiz/hint`
Get AI-generated hint for a question.

**Request Body:**
```json
{
  "questionId": 1
}
```

#### GET `/api/quiz/history`
Get quiz history with filtering options.

**Query Parameters:**
- `grade`: Filter by grade level
- `subject`: Filter by subject
- `minMarks`: Minimum score filter
- `maxMarks`: Maximum score filter
- `fromDate`: Start date filter (YYYY-MM-DD)
- `toDate`: End date filter (YYYY-MM-DD)
- `pageNumber`: Page number (default: 1)
- `pageSize`: Items per page (default: 10)

#### POST `/api/quiz/retry`
Retry a quiz with adaptive difficulty.

**Request Body:**
```json
{
  "quizId": 1
}
```

#### GET `/api/quiz/leaderboard`
Get leaderboard for top performers.

**Query Parameters:**
- `grade`: Filter by grade level
- `subject`: Filter by subject
- `top`: Number of top entries (default: 10)

## 🧪 Testing

### Using Swagger UI

The application provides a comprehensive Swagger UI at the root URL (`/`) with:

1. **Interactive Documentation**
   - All API endpoints fully documented
   - Request/response schemas
   - JWT authentication support

2. **Try It Out Feature**
   - Test API endpoints directly from the browser
   - Automatically pass authentication tokens
   - View detailed responses

### Sample Test Flow

1. **Login**
   ```
   POST /api/auth/login
   Body: {"username": "testuser", "password": "test123"}
   ```

2. **Generate Quiz**
   ```
   POST /api/quiz/generate
   Headers: Authorization: Bearer {token}
   Body: {"subject": "Mathematics", "gradeLevel": "Grade 5", "numberOfQuestions": 3}
   ```

3. **Submit Quiz**
   ```
   POST /api/quiz/submit
   Headers: Authorization: Bearer {token}
   Body: {"quizId": 1, "answers": [...]}
   ```

## 📊 Database Schema

The application uses the following main entities:

- **Users**: User accounts and authentication
- **Quizzes**: Quiz metadata and configuration
- **Questions**: Individual quiz questions with difficulty levels
- **Answers**: Multiple choice options for questions
- **QuizSubmissions**: User quiz attempts and scores
- **SubmissionAnswers**: Individual question responses

See `database/init.sql` for complete schema and sample data.

## 🎯 Performance Features

### Caching Strategy
- **Redis Integration**: Caches frequently accessed data
- **Cache Keys**: Strategic caching of quiz content, hints, and leaderboards
- **TTL Management**: Appropriate cache expiration times

### Database Optimization
- **Indexes**: Strategic database indexing for performance
- **Connection Pooling**: Efficient database connection management
- **Query Optimization**: Optimized LINQ queries with proper includes

## 🚦 Known Issues

1. **AI Integration**: Requires valid Groq API key for full functionality
2. **Database Dependencies**: Application requires PostgreSQL and Redis to be running
3. **Mock Authentication**: Current authentication accepts any credentials (by design)
4. **CORS Configuration**: Currently allows all origins (development setting)

## 🔐 Security Considerations

- JWT tokens with configurable expiration
- Password hashing should be implemented for production
- API rate limiting should be added
- Input validation and sanitization
- HTTPS enforcement recommended for production

## 📈 Monitoring & Logging

- **Structured Logging**: Uses .NET Core logging framework
- **Error Handling**: Comprehensive error handling with appropriate HTTP status codes
- **Performance Metrics**: Built-in performance monitoring capabilities

## 🚀 Deployment

### Web Hosting Deployment

1. **Publish the application**
   ```bash
   dotnet publish -c Release
   ```

2. **Deploy to hosting service**
   - Copy the published files to your web hosting provider
   - Configure environment variables for database connection and API keys
   - Ensure PostgreSQL database is accessible

### Cloud Deployment Options

- **Azure App Service**: Native .NET deployment
- **AWS Elastic Beanstalk**: .NET support
- **Heroku**: .NET Core buildpack
- **DigitalOcean App Platform**: Git-based deployment

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🙏 Acknowledgments

- **Groq**: AI API integration
- **PostgreSQL**: Robust database engine
- **Redis**: High-performance caching
- **.NET Team**: Excellent framework and tooling

## 📞 Support

For issues and questions:
1. Check the [Issues](link-to-issues) section
2. Review the documentation
3. Contact the development team

---

**Happy Quizzing! 🎓✨**
