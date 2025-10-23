## Clone repository

```bash
git clone https://github.com/yourusername/TaskManagementAPI.git
cd TaskManagementAPI
```

## Create Database

### Using Visual Studio:

1. Open Package Manager Console (Tools → NuGet Package Manager → Package Manager Console).

2. Run the following command:

```bash
Update-Database -Project TaskManagement.Data.Migrations -StartupProject TaskManagementAPI
```

## Run the Application

Set `TaskManagementAPI` & `TaskManagementSystem` as startup projects

### Default Admin Account

Username: `Admin`

Password: `Admin@123`
