using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using TaskManagement.Data.Migrations.Models;
using TaskManagementAPI.Models.Project;
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

    public async Task<IActionResult> Index(int page = 1)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var token = await GenerateJwtToken(user);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/Project?page={page}&pageSize=15";

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

    public async Task<IActionResult> ProjectDetails(Guid id)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var token = await GenerateJwtToken(user);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/Project/{id}?modules=ProjectUser&modules=Tasks&modules=TaskAttachment&modules=TaskUser";
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
            users = users
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
    public async Task<IActionResult> CreateTask(string title, int progressStatus, string projectId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(new { success = false, message = "User not found." });

            if (string.IsNullOrWhiteSpace(title))
                return Json(new { success = false, message = "Task title is required." });

            if (string.IsNullOrWhiteSpace(projectId))
                return Json(new { success = false, message = "Project ID is required." });

            if (!Guid.TryParse(projectId, out Guid projectGuid))
                return Json(new { success = false, message = "Invalid Project ID format." });

            var token = await GenerateJwtToken(user);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var apiUrl = $"{_configuration["APIURL"].TrimEnd('/')}/api/ProjectTask";

            var payload = new
            {
                Title = title,
                ProgressStatus = progressStatus,
                ProjectId = projectGuid
            };

            var response = await client.PostAsJsonAsync(apiUrl, payload);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return Json(new
                {
                    success = false,
                    message = $"Failed to create task. API returned: {response.StatusCode}"
                });
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var resultJson = System.Text.Json.JsonDocument.Parse(responseContent);

            if (resultJson.RootElement.TryGetProperty("projectTask", out var taskElement))
            {
                var taskData = new
                {
                    Id = taskElement.GetProperty("id").GetGuid(),
                    Title = taskElement.GetProperty("title").GetString(),
                    Description = taskElement.TryGetProperty("description", out var desc) ? desc.GetString() : "",
                    ProjectId = taskElement.GetProperty("projectId").GetGuid(),
                    ProgressStatus = taskElement.GetProperty("progressStatus").GetInt32(),
                    StartDate = taskElement.TryGetProperty("startDate", out var startDate) && startDate.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? startDate.GetDateTime()
                        : (DateTime?)null,
                    DueDate = taskElement.TryGetProperty("dueDate", out var dueDate) && dueDate.ValueKind != System.Text.Json.JsonValueKind.Null
                        ? dueDate.GetDateTime()
                        : (DateTime?)null
                };

                return Json(new
                {
                    success = true,
                    message = "Task created successfully!",
                    task = taskData
                });
            }
            else
            {
                return Json(new
                {
                    success = true,
                    message = "Task created successfully!"
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
}
