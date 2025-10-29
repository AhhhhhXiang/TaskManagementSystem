using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Data.Migrations.Models;
using TaskManagementAPI.Models.Project;
using TaskManagementAPI.Models.ProjectTask;
using TaskManagementAPI.Models.ProjectUser;
using TaskManagementSystem.Models.ViewModels;

public class ProjectController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly IConfiguration _configuration;

    public ProjectController(UserManager<IdentityUser> userManager, IConfiguration configuration)
    {
        _userManager = userManager;
        _configuration = configuration;
    }

    public async Task<IActionResult> Index(int page = 1, string projectName = "", string memberName = "", string priority = "")
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var token = await GenerateJwtToken(user);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/Project?page={page}&pageSize=14&modules=ProjectUser&modules=Tasks&modules=TaskUser";

        if (!string.IsNullOrEmpty(projectName))
            apiUrl += $"&projectName={Uri.EscapeDataString(projectName)}";
        if (!string.IsNullOrEmpty(memberName))
            apiUrl += $"&memberName={Uri.EscapeDataString(memberName)}";
        if (!string.IsNullOrEmpty(priority))
            apiUrl += $"&priority={Uri.EscapeDataString(priority)}";

        var response = await client.GetAsync(apiUrl);
        if (!response.IsSuccessStatusCode)
            return View("Error");

        var apiResponse = await response.Content.ReadFromJsonAsync<GetAllProjectsResponse>();
        if (apiResponse == null)
            return View(new PaginatedProjectsViewModel());

        var viewModel = new PaginatedProjectsViewModel
        {
            Projects = apiResponse.projects ?? new List<ProjectsResponse>(),
            Page = apiResponse.page,
            PageSize = apiResponse.pageSize,
            TotalCount = apiResponse.totalCount
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportProjectsToCSV()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/Project?page=1&pageSize=10000&modules=ProjectUser&modules=Tasks&modules=TaskUser";

            var response = await client.GetAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            var apiResponse = await response.Content.ReadFromJsonAsync<GetAllProjectsResponse>();
            if (apiResponse == null || apiResponse.projects == null)
            {
                return Json(new { success = false, message = "No projects found to export." });
            }

            var csvContent = GenerateSimplifiedProjectsCSV(apiResponse.projects);

            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            var filename = $"projects_export_{timestamp}.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", filename);
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    private string GenerateSimplifiedProjectsCSV(List<ProjectsResponse> projects)
    {
        var csv = new StringBuilder();

        csv.AppendLine("Project Name,Tasks,Task Priorities,Task Deadlines,Members");

        foreach (var project in projects)
        {
            var allTaskNames = new List<string>();
            var allTaskPriorities = new List<string>();
            var allTaskDeadlines = new List<string>();

            if (project.projectTasks != null)
            {
                foreach (var task in project.projectTasks)
                {
                    allTaskNames.Add(task.Title ?? "Untitled Task");
                    allTaskPriorities.Add(task.PriorityStatus.ToString() ?? "Not Set");
                    allTaskDeadlines.Add(task.DueDate?.ToString("yyyy-MM-dd") ?? "No Deadline");
                }
            }

            var allMemberNames = new List<string>();
            if (project.projectUsers != null)
            {
                allMemberNames.AddRange(project.projectUsers
                    .Where(pu => !string.IsNullOrEmpty(pu.UserName))
                    .Select(pu => pu.UserName));
            }

            var tasksString = string.Join("; ", allTaskNames);
            var prioritiesString = string.Join("; ", allTaskPriorities);
            var deadlinesString = string.Join("; ", allTaskDeadlines);
            var membersString = string.Join("; ", allMemberNames);

            csv.AppendLine(
                $"\"{EscapeCsvField(project.Name)}\"," +
                $"\"{EscapeCsvField(tasksString)}\"," +
                $"\"{EscapeCsvField(prioritiesString)}\"," +
                $"\"{EscapeCsvField(deadlinesString)}\"," +
                $"\"{EscapeCsvField(membersString)}\""
            );
        }

        return csv.ToString();
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field)) return "";
        return field.Replace("\"", "\"\"");
    }

    private async Task<string> GenerateJwtToken(IdentityUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? ""),
            new Claim(ClaimTypes.Email, user.Email ?? "")
        };

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProject([FromForm] string projectName, [FromForm] string projectDescription)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var token = await GenerateJwtToken(user);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/Project";

        var payload = new
        {
            Name = projectName,
            Description = projectDescription
        };

        var response = await client.PostAsJsonAsync(apiUrl, payload);

        if (!response.IsSuccessStatusCode)
        {
            return Json(new { success = false, message = "Failed to create project." });
        }

        var resultJson = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (!resultJson.TryGetProperty("project", out var projectElement) ||
            !projectElement.TryGetProperty("id", out var idElement))
        {
            return Json(new { success = false, message = "Failed to retrieve project ID from API." });
        }

        var projectId = idElement.GetGuid();

        return Json(new
        {
            success = true,
            message = "Project created successfully!",
            projectId = projectId
        });
    }

    public async Task<IActionResult> ProjectDetails(Guid id, [FromQuery] string? taskName, [FromQuery] DateTime? taskStartDate,
    [FromQuery] DateTime? taskEndDate, [FromQuery] string? taskPriority, [FromQuery] string? taskMemberName,
    [FromQuery] string? taskSortBy = "Title", [FromQuery] string? taskSortOrder = "asc",
    [FromQuery] int taskPage = 1, [FromQuery] int taskPageSize = 10)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var token = await GenerateJwtToken(user);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var apiUrlBuilder = new StringBuilder($"{_configuration["APIURL"].TrimEnd('/')}/api/Project/{id}");
        apiUrlBuilder.Append("?modules=ProjectUser&modules=Tasks&modules=TaskAttachment&modules=TaskUser&modules=TaskComment");

        if (!string.IsNullOrEmpty(taskName))
            apiUrlBuilder.Append($"&taskName={Uri.EscapeDataString(taskName)}");

        if (taskStartDate.HasValue)
            apiUrlBuilder.Append($"&taskStartDate={taskStartDate.Value:yyyy-MM-dd}");

        if (taskEndDate.HasValue)
            apiUrlBuilder.Append($"&taskEndDate={taskEndDate.Value:yyyy-MM-dd}");

        if (!string.IsNullOrEmpty(taskPriority))
            apiUrlBuilder.Append($"&taskPriority={Uri.EscapeDataString(taskPriority)}");

        if (!string.IsNullOrEmpty(taskMemberName))
            apiUrlBuilder.Append($"&taskMemberName={Uri.EscapeDataString(taskMemberName)}");

        apiUrlBuilder.Append($"&taskSortBy={Uri.EscapeDataString(taskSortBy)}");
        apiUrlBuilder.Append($"&taskSortOrder={Uri.EscapeDataString(taskSortOrder)}");

        apiUrlBuilder.Append($"&taskPage={taskPage}");
        apiUrlBuilder.Append($"&taskPageSize={taskPageSize}");

        var apiUrl = apiUrlBuilder.ToString();
        var response = await client.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
            return View("Error");

        var getProjectResponse = await response.Content.ReadFromJsonAsync<GetProjectResponse>();
        if (getProjectResponse == null || getProjectResponse.project == null)
            return View("Error");

        var project = getProjectResponse.project;

        List<UserReturnModel>? users = null;

        if (await _userManager.IsInRoleAsync(user, "Administrator"))
        {
            var registeredUsers = await _userManager.GetUsersInRoleAsync("RegisterUser");

            users = registeredUsers.Select(u => new UserReturnModel
            {
                Id = Guid.Parse(u.Id),
                UserName = u.UserName,
                Email = u.Email
            }).ToList();
        }

        var viewModel = new ProjectDetailsViewModel
        {
            project = project,
            users = users,
            currentUserId = Guid.Parse(user.Id),
            TaskFilter = new TaskFilterViewModel
            {
                TaskName = taskName,
                TaskStartDate = taskStartDate,
                TaskEndDate = taskEndDate,
                TaskPriority = taskPriority,
                TaskMemberName = taskMemberName,
                TaskSortBy = taskSortBy,
                TaskSortOrder = taskSortOrder,
                TaskPage = taskPage,
                TaskPageSize = taskPageSize,
                TotalTaskCount = project.TotalTaskCount
            }
        };

        return View(viewModel);
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateProjectName(Guid id, string projectName)
    {
        if (id == Guid.Empty || string.IsNullOrWhiteSpace(projectName))
            return Json(new { success = false, message = "Invalid project ID or name." });

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return Challenge();

        var token = await GenerateJwtToken(user);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/Project/{id}";

        var payload = new { Name = projectName };

        var patchMethod = new HttpMethod("PATCH");
        var request = new HttpRequestMessage(patchMethod, apiUrl)
        {
            Content = JsonContent.Create(payload)
        };

        var response = await client.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var errorText = await response.Content.ReadAsStringAsync();
            return Json(new { success = false, message = $"API error: {errorText}" });
        }

        return Json(new { success = true, message = "Project name updated successfully." });
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask(
        [FromForm] string title,
        [FromForm] string description,
        [FromForm] string dueDate,
        [FromForm] string priorityStatus,
        [FromForm] int progressStatus,
        [FromForm] string projectId,
        [FromForm] List<string> assigneeIds = null,
        [FromForm] List<IFormFile> attachments = null)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(title))
                return Json(new { success = false, message = "Task title is required." });

            if (string.IsNullOrWhiteSpace(projectId) || !Guid.TryParse(projectId, out Guid projectGuid))
                return Json(new { success = false, message = "Valid Project ID is required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectTask";

            DateTime? parsedDueDate = null;
            if (!string.IsNullOrEmpty(dueDate) && DateTime.TryParse(dueDate, out DateTime tempDueDate))
            {
                parsedDueDate = tempDueDate;
            }

            if (!int.TryParse(priorityStatus, out int priority) || priority < 1 || priority > 3)
            {
                priority = 1;
            }

            // Create the task first
            var taskPayload = new
            {
                Title = title.Trim(),
                Description = description?.Trim(),
                ProgressStatus = progressStatus,
                ProjectId = projectGuid,
                DueDate = parsedDueDate,
                PriorityStatus = priority,
                CreatedBy = Guid.Parse(user.Id)
            };

            var taskResponse = await client.PostAsJsonAsync(apiUrl, taskPayload);

            if (!taskResponse.IsSuccessStatusCode)
            {
                var errorText = await taskResponse.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"Failed to create task: {errorText}" });
            }

            var taskResponseContent = await taskResponse.Content.ReadAsStringAsync();
            var taskResultJson = System.Text.Json.JsonDocument.Parse(taskResponseContent);

            if (!taskResultJson.RootElement.TryGetProperty("projectTask", out var taskElement))
            {
                return Json(new { success = false, message = "Invalid response from task creation API." });
            }

            var taskId = taskElement.GetProperty("id").GetGuid();
            var createdTask = new
            {
                Id = taskId,
                Title = taskElement.GetProperty("title").GetString(),
                Description = taskElement.TryGetProperty("description", out var desc) ? desc.GetString() : description,
                ProjectId = projectGuid,
                ProgressStatus = progressStatus,
                PriorityStatus = priority,
                DueDate = parsedDueDate,
                CreatedDateTime = DateTime.UtcNow,
                CreatedBy = Guid.Parse(user.Id)
            };

            // Add members to the task
            if (assigneeIds != null && assigneeIds.Count > 0)
            {
                foreach (var assigneeId in assigneeIds)
                {
                    if (Guid.TryParse(assigneeId, out Guid userId))
                    {
                        try
                        {
                            var memberApiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/TaskUser";
                            var memberPayload = new { TaskId = taskId.ToString(), UserId = assigneeId };
                            var memberResponse = await client.PostAsJsonAsync(memberApiUrl, memberPayload);

                            if (!memberResponse.IsSuccessStatusCode)
                            {
                                var errorText = await memberResponse.Content.ReadAsStringAsync();
                                Console.WriteLine($"Failed to add member {assigneeId} to task: {errorText}");
                            }
                            else
                            {
                                var memberResponseContent = await memberResponse.Content.ReadAsStringAsync();
                                var memberResultJson = System.Text.Json.JsonDocument.Parse(memberResponseContent);

                                if (memberResultJson.RootElement.TryGetProperty("success", out var successElement) && !successElement.GetBoolean())
                                {
                                    var message = memberResultJson.RootElement.TryGetProperty("message", out var messageElement)
                                        ? messageElement.GetString()
                                        : "Unknown error";
                                    Console.WriteLine($"Failed to add member {assigneeId} to task: {message}");
                                }
                                else
                                {
                                    Console.WriteLine($"Successfully added member {assigneeId} to task");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error adding member {assigneeId} to task: {ex.Message}");
                        }
                    }
                }
            }

            // Upload and add attachments
            var uploadedAttachments = new List<object>();
            if (attachments != null && attachments.Count > 0)
            {
                foreach (var file in attachments)
                {
                    if (file.Length > 0)
                    {
                        try
                        {
                            // Upload file
                            var uploadFormData = new MultipartFormDataContent();
                            var fileContent = new StreamContent(file.OpenReadStream());
                            fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                            uploadFormData.Add(fileContent, "file", file.FileName);

                            var uploadApiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/Attachment/Upload";
                            var uploadResponse = await client.PostAsync(uploadApiUrl, uploadFormData);

                            if (!uploadResponse.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Upload failed for {file.FileName}: {uploadResponse.StatusCode}");
                                continue;
                            }

                            var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
                            if (!uploadResult.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
                            {
                                var message = uploadResult.TryGetProperty("message", out var msgProp)
                                    ? msgProp.GetString()
                                    : "Upload failed";
                                Console.WriteLine($"Upload failed for {file.FileName}: {message}");
                                continue;
                            }

                            var fileName = uploadResult.GetProperty("fileName").GetString();
                            var path = uploadResult.GetProperty("path").GetString();

                            var attachmentPayload = new
                            {
                                TaskId = taskId.ToString(),
                                FileName = fileName,
                                FilePath = path
                            };

                            var attachmentApiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/TaskAttachment";
                            var attachmentResponse = await client.PostAsJsonAsync(attachmentApiUrl, attachmentPayload);

                            if (!attachmentResponse.IsSuccessStatusCode)
                            {
                                Console.WriteLine($"Failed to create attachment record for {file.FileName}");
                                continue;
                            }

                            uploadedAttachments.Add(new
                            {
                                fileName = fileName,
                                path = path,
                                originalName = file.FileName,
                                size = file.Length
                            });
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing attachment {file.FileName}: {ex.Message}");
                        }
                    }
                }
            }

            return Json(new
            {
                success = true,
                message = "Task created successfully!",
                task = createdTask,
                attachments = uploadedAttachments
            });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTaskTitle([FromForm] string taskId, [FromForm] string title)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(title))
                return Json(new { success = false, message = "Task ID and title are required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectTask/{taskId}";

            var payload = new { Title = title };

            var patchMethod = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(patchMethod, apiUrl)
            {
                Content = JsonContent.Create(payload)
            };

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            return Json(new { success = true, message = "Task title updated successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTaskDescription([FromForm] string taskId, [FromForm] string description)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(taskId))
                return Json(new { success = false, message = "Task ID is required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectTask/{taskId}";

            var payload = new { Description = description ?? string.Empty };

            var patchMethod = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(patchMethod, apiUrl)
            {
                Content = JsonContent.Create(payload)
            };

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            return Json(new { success = true, message = "Task description updated successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> MoveTask([FromForm] string taskId, [FromForm] int progressStatus)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(taskId))
                return Json(new { success = false, message = "Task ID is required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectTask/{taskId}";

            var payload = new { ProgressStatus = progressStatus };

            var patchMethod = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(patchMethod, apiUrl)
            {
                Content = JsonContent.Create(payload)
            };

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            return Json(new { success = true, message = "Task moved successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveTask([FromForm] string taskId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(taskId))
                return Json(new { success = false, message = "Task ID is required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectTask/{taskId}";

            var response = await client.DeleteAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            return Json(new { success = true, message = "Task removed successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddTaskMember([FromForm] string taskId, [FromForm] string userId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(userId))
                return Json(new { success = false, message = "Task ID and User ID are required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/TaskUser";

            var payload = new
            {
                TaskId = taskId,
                UserId = userId
            };

            var response = await client.PostAsJsonAsync(apiUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var resultJson = System.Text.Json.JsonDocument.Parse(responseContent);

            if (resultJson.RootElement.TryGetProperty("success", out var successElement) &&
                !successElement.GetBoolean())
            {
                var message = resultJson.RootElement.TryGetProperty("message", out var messageElement)
                    ? messageElement.GetString()
                    : "Unknown error";
                return Json(new { success = false, message });
            }

            return Json(new { success = true, message = "Member added to task successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveTaskMember([FromForm] string taskId, [FromForm] string userId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(userId))
                return Json(new { success = false, message = "Task ID and User ID are required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var getApiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/TaskUser?TaskId={taskId}&UserId={userId}";
            var getResponse = await client.GetAsync(getApiUrl);

            if (!getResponse.IsSuccessStatusCode)
            {
                var errorText = await getResponse.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error getting task user: {errorText}" });
            }

            var getResponseContent = await getResponse.Content.ReadAsStringAsync();
            var getResultJson = System.Text.Json.JsonDocument.Parse(getResponseContent);

            if (!getResultJson.RootElement.TryGetProperty("taskUsers", out var taskUsersElement) ||
                taskUsersElement.GetArrayLength() == 0)
            {
                return Json(new { success = false, message = "Task user assignment not found." });
            }

            var taskUserId = taskUsersElement.EnumerateArray().First().GetProperty("id").GetInt32();

            var deleteApiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/TaskUser/{taskUserId}";
            var deleteResponse = await client.DeleteAsync(deleteApiUrl);

            if (!deleteResponse.IsSuccessStatusCode)
            {
                var errorText = await deleteResponse.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error deleting task user: {errorText}" });
            }

            return Json(new { success = true, message = "Member removed from task successfully." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UploadAttachments([FromForm] List<IFormFile> files)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (files == null || files.Count == 0)
                return Json(new { success = false, message = "No files uploaded." });

            var token = await GenerateJwtToken(user);
            var uploadResults = new List<object>();

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            foreach (var file in files)
            {
                if (file.Length == 0)
                    continue;

                try
                {
                    var uploadFormData = new MultipartFormDataContent();
                    var fileContent = new StreamContent(file.OpenReadStream());
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
                    uploadFormData.Add(fileContent, "file", file.FileName);

                    var uploadApiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/Attachment/Upload";
                    var uploadResponse = await client.PostAsync(uploadApiUrl, uploadFormData);

                    if (!uploadResponse.IsSuccessStatusCode)
                    {
                        uploadResults.Add(new
                        {
                            fileName = file.FileName,
                            success = false,
                            message = $"Upload failed: {uploadResponse.StatusCode}"
                        });
                        continue;
                    }

                    var uploadResult = await uploadResponse.Content.ReadFromJsonAsync<JsonElement>();
                    if (!uploadResult.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
                    {
                        var message = uploadResult.TryGetProperty("message", out var msgProp)
                            ? msgProp.GetString()
                            : "Upload failed";
                        uploadResults.Add(new
                        {
                            fileName = file.FileName,
                            success = false,
                            message = message
                        });
                        continue;
                    }

                    var fileName = uploadResult.GetProperty("fileName").GetString();
                    var path = uploadResult.GetProperty("path").GetString();

                    uploadResults.Add(new
                    {
                        fileName = fileName,
                        path = path,
                        success = true,
                        originalName = file.FileName,
                        size = file.Length
                    });
                }
                catch (Exception ex)
                {
                    uploadResults.Add(new
                    {
                        fileName = file.FileName,
                        success = false,
                        message = $"Error: {ex.Message}"
                    });
                }
            }

            return Json(new
            {
                success = true,
                message = $"Processed {files.Count} files",
                results = uploadResults
            });
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"Upload error: {ex.Message}"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateTaskAttachment([FromForm] string taskId, [FromForm] string fileName, [FromForm] string filePath)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(fileName) || string.IsNullOrWhiteSpace(filePath))
                return Json(new { success = false, message = "TaskId, FileName and FilePath are required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/TaskAttachment";

            var payload = new
            {
                TaskId = taskId,
                FileName = fileName,
                FilePath = filePath
            };

            var response = await client.PostAsJsonAsync(apiUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            if (result.TryGetProperty("taskAttachment", out var taskAttachmentElement))
            {
                var attachmentData = new
                {
                    Id = taskAttachmentElement.GetProperty("id").GetInt64(),
                    TaskId = taskAttachmentElement.GetProperty("taskId").GetGuid(),
                    FileName = taskAttachmentElement.GetProperty("fileName").GetString(),
                    FilePath = $"{_configuration["APIURL"].TrimEnd('/')}/api/Attachment/GetAttachment?attachmentId={taskAttachmentElement.GetProperty("id").GetInt64()}"
                };

                return Json(new
                {
                    success = true,
                    message = "Attachment created successfully",
                    taskAttachment = attachmentData
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = "Failed to create task attachment record"
                });
            }
        }
        catch (Exception ex)
        {
            return Json(new
            {
                success = false,
                message = $"An error occurred: {ex.Message}"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveAttachment([FromForm] string attachmentId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(attachmentId))
                return Json(new { success = false, message = "Attachment ID is required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/TaskAttachment/{attachmentId}";

            Console.WriteLine($"Calling API to delete attachment: {apiUrl}");

            var response = await client.DeleteAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"API deletion failed: {errorText}");
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"API deletion response: {responseContent}");

            try
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(responseContent);
                if (result.TryGetProperty("success", out var successProp) && successProp.GetBoolean())
                {
                    return Json(new { success = true, message = "Attachment deleted successfully" });
                }
                else
                {
                    var message = result.TryGetProperty("message", out var messageProp)
                        ? messageProp.GetString()
                        : "Unknown error from API";
                    return Json(new { success = false, message });
                }
            }
            catch (JsonException)
            {
                return Json(new { success = true, message = "Attachment deleted successfully" });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in RemoveAttachment: {ex}");
            return Json(new
            {
                success = false,
                message = $"An error occurred: {ex.Message}"
            });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddProjectMember(Guid projectId, Guid userId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            if (!await _userManager.IsInRoleAsync(user, "Administrator"))
                return Json(new { success = false, message = "Only administrators can add members" });

            var token = await GenerateJwtToken(user);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectUser";
            var createRequest = new
            {
                ProjectId = projectId.ToString(),
                UserId = userId.ToString()
            };

            var jsonContent = JsonSerializer.Serialize(createRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(apiUrl, content);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Member added to project successfully" });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = "Failed to add member: " + errorContent });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in AddProjectMember: {ex}");
            return Json(new { success = false, message = "Internal server error: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RemoveProjectMember(Guid projectId, Guid userId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            if (!await _userManager.IsInRoleAsync(user, "Administrator"))
                return Json(new { success = false, message = "Only administrators can remove members" });

            var token = await GenerateJwtToken(user);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var getApiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectUser?ProjectId={projectId}";
            var getResponse = await client.GetAsync(getApiUrl);

            if (!getResponse.IsSuccessStatusCode)
            {
                return Json(new { success = false, message = "Failed to find project members" });
            }

            var projectUserData = await getResponse.Content.ReadFromJsonAsync<GetAllProjectUsersResponse>();

            if (projectUserData?.projectUsers == null || projectUserData.projectUsers.Count == 0)
            {
                return Json(new { success = false, message = "No project members found" });
            }

            var projectUser = projectUserData.projectUsers
                .FirstOrDefault(pu => pu.UserId == userId);

            if (projectUser == null)
            {
                return Json(new { success = false, message = "User is not a member of this project" });
            }

            var deleteApiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectUser/{projectUser.Id}";
            var deleteResponse = await client.DeleteAsync(deleteApiUrl);

            if (deleteResponse.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Member removed from project successfully" });
            }
            else
            {
                var errorContent = await deleteResponse.Content.ReadAsStringAsync();
                return Json(new { success = false, message = "Failed to remove member: " + errorContent });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in RemoveProjectMember: {ex}");
            return Json(new { success = false, message = "Internal server error: " + ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteProject([FromBody] string projectId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found" });

            if (!await _userManager.IsInRoleAsync(user, "Administrator"))
                return Json(new { success = false, message = "Only administrators can delete projects" });

            var token = await GenerateJwtToken(user);
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/Project/{projectId}";
            var response = await client.DeleteAsync(apiUrl);

            if (response.IsSuccessStatusCode)
            {
                return Json(new { success = true, message = "Project deleted successfully" });
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = "Failed to delete project: " + errorContent });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTaskPriority(string taskId, int priorityStatus)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(taskId))
                return Json(new { success = false, message = "Task ID is required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectTask/{taskId}";

            var payload = new { PriorityStatus = priorityStatus };

            var patchMethod = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(patchMethod, apiUrl)
            {
                Content = JsonContent.Create(payload)
            };

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            return Json(new { success = true, message = "Task Priority Status changed." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }    
    
    [HttpPost]
    public async Task<IActionResult> UpdateTaskDueDate(string taskId, string dueDate)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(taskId))
                return Json(new { success = false, message = "Task ID is required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectTask/{taskId}";

            var payload = new { DueDate = dueDate };

            var patchMethod = new HttpMethod("PATCH");
            var request = new HttpRequestMessage(patchMethod, apiUrl)
            {
                Content = JsonContent.Create(payload)
            };

            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            return Json(new { success = true, message = "Task Due Date changed." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateTaskComment([FromForm] string taskId, [FromForm] string comment)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(taskId) || string.IsNullOrWhiteSpace(comment))
                return Json(new { success = false, message = "Task ID and Comment are required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/TaskComment";

            var payload = new
            {
                TaskId = taskId,
                UserId = user.Id,
                Comment = comment
            };

            var response = await client.PostAsJsonAsync(apiUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            try
            {
                var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

                if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("taskComment", out var taskCommentElement))
                {
                    var commentData = new
                    {
                        Id = taskCommentElement.TryGetProperty("id", out var idElement) ? idElement.GetInt64() : 0,
                        TaskId = taskCommentElement.TryGetProperty("taskId", out var taskIdElement) ? taskIdElement.GetGuid() : Guid.Parse(taskId),
                        UserId = Guid.Parse(user.Id),
                        Username = user.UserName ?? "Unknown User",
                        Comment = taskCommentElement.TryGetProperty("comment", out var commentTextElement) ? commentTextElement.GetString() : comment,
                        CreatedDateTime = taskCommentElement.TryGetProperty("createdDateTime", out var dateElement) ? dateElement.GetDateTime() : DateTime.UtcNow
                    };

                    return Json(new
                    {
                        success = true,
                        message = "Comment added successfully",
                        comment = commentData
                    });
                }
                else
                {
                    var commentData = new
                    {
                        Id = result.TryGetProperty("id", out var idElement) ? idElement.GetInt64() : 0,
                        TaskId = Guid.Parse(taskId),
                        UserId = Guid.Parse(user.Id),
                        Username = user.UserName ?? "Unknown User",
                        Comment = result.TryGetProperty("comment", out var commentTextElement) ? commentTextElement.GetString() : comment,
                        CreatedDateTime = result.TryGetProperty("createdDateTime", out var dateElement) ? dateElement.GetDateTime() : DateTime.UtcNow
                    };

                    return Json(new
                    {
                        success = true,
                        message = "Comment added successfully",
                        comment = commentData
                    });
                }
            }
            catch (JsonException jsonEx)
            {
                var commentData = new
                {
                    Id = 0,
                    TaskId = Guid.Parse(taskId),
                    UserId = Guid.Parse(user.Id),
                    Username = user.UserName ?? "Unknown User",
                    Comment = comment,
                    CreatedDateTime = DateTime.UtcNow
                };

                return Json(new
                {
                    success = true,
                    message = "Comment added successfully",
                    comment = commentData
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Exception in CreateTaskComment: {ex}");
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpDelete]
    public async Task<IActionResult> RemoveTaskComment([FromQuery] int commentId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (commentId <= 0)
                return Json(new { success = false, message = "Comment ID is required." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/TaskComment/{commentId}";

            var response = await client.DeleteAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                return Json(new { success = false, message = $"API error: {errorText}" });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(responseContent);

            if (result.TryGetProperty("success", out var successProperty) &&
                successProperty.GetBoolean())
            {
                return Json(new { success = true, message = "Comment deleted successfully." });
            }
            else
            {
                var message = result.TryGetProperty("message", out var messageProperty)
                    ? messageProperty.GetString()
                    : "Failed to delete comment";
                return Json(new { success = false, message });
            }
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllProjectTasks(Guid projectId,
    [FromQuery] string? taskName = null,
    [FromQuery] DateTime? taskStartDate = null,
    [FromQuery] DateTime? taskEndDate = null,
    [FromQuery] string? taskPriority = null,
    [FromQuery] string? taskMemberName = null,
    [FromQuery] string? taskSortBy = "Title",
    [FromQuery] string? taskSortOrder = "asc",
    [FromQuery] int taskPage = 1,
    [FromQuery] int taskPageSize = 10,
    [FromQuery] bool export = false)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var token = await GenerateJwtToken(user);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var apiUrlBuilder = new StringBuilder($"{_configuration["APIURL"].TrimEnd('/')}/api/Project/{projectId}");
        apiUrlBuilder.Append("?modules=ProjectUser&modules=Tasks&modules=TaskAttachment&modules=TaskUser&modules=TaskComment");

        if (!string.IsNullOrEmpty(taskName))
            apiUrlBuilder.Append($"&taskName={Uri.EscapeDataString(taskName)}");

        if (taskStartDate.HasValue)
            apiUrlBuilder.Append($"&taskStartDate={taskStartDate.Value:yyyy-MM-dd}");

        if (taskEndDate.HasValue)
            apiUrlBuilder.Append($"&taskEndDate={taskEndDate.Value:yyyy-MM-dd}");

        if (!string.IsNullOrEmpty(taskPriority))
            apiUrlBuilder.Append($"&taskPriority={Uri.EscapeDataString(taskPriority)}");

        if (!string.IsNullOrEmpty(taskMemberName))
            apiUrlBuilder.Append($"&taskMemberName={Uri.EscapeDataString(taskMemberName)}");

        if (export)
        {
            apiUrlBuilder.Append($"&taskPage=1");
            apiUrlBuilder.Append($"&taskPageSize=10000");
        }
        else
        {
            apiUrlBuilder.Append($"&taskPage={taskPage}");
            apiUrlBuilder.Append($"&taskPageSize={taskPageSize}");
        }

        apiUrlBuilder.Append($"&taskSortBy={Uri.EscapeDataString(taskSortBy)}");
        apiUrlBuilder.Append($"&taskSortOrder={Uri.EscapeDataString(taskSortOrder)}");

        var apiUrl = apiUrlBuilder.ToString();
        var response = await client.GetAsync(apiUrl);

        if (!response.IsSuccessStatusCode)
            return Json(new { success = false, message = "Failed to fetch tasks" });

        var getProjectResponse = await response.Content.ReadFromJsonAsync<GetProjectResponse>();
        if (getProjectResponse?.project?.projectTasks == null)
            return Json(new { success = false, message = "No tasks found" });

        return Json(new
        {
            success = true,
            tasks = getProjectResponse.project.projectTasks,
            totalCount = getProjectResponse.project.TotalTaskCount
        });
    }
}
