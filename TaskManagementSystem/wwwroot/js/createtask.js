// ========== Add Task ==========

let newTaskData = {
    title: '',
    description: '',
    priority: '1', // Changed to use numeric values like the main system
    status: 1, // Default to "To Do"
    dueDate: '',
    assigneeIds: []
};

function openAddTaskModal() {
    const modal = document.getElementById('addTaskModal');
    modal.classList.add('active');
    document.body.style.overflow = 'hidden';

    resetAddTaskForm();
    setupAddTaskFileUpload(); // Initialize file upload

    setTimeout(() => {
        const titleInput = document.getElementById('addTaskTitle');
        titleInput.focus();

        titleInput.addEventListener('input', validateAddTaskForm);

        // Initialize the date picker for add task modal
        initAddTaskDatePicker();
    }, 100);
}

function initAddTaskDatePicker() {
    const addTaskDueDateInput = document.getElementById('addTaskDueDate');
    if (addTaskDueDateInput) {
        // Remove any existing event listeners to avoid duplicates
        addTaskDueDateInput.removeEventListener('click', handleAddTaskDateClick);
        addTaskDueDateInput.addEventListener('click', handleAddTaskDateClick);

        // Initialize clear button visibility
        updateClearButtonVisibility(addTaskDueDateInput);
    }
}

function handleAddTaskDateClick(e) {
    e.stopPropagation();
    openAddTaskDatePicker();
}

function validateAddTaskForm() {
    const title = document.getElementById('addTaskTitle').value.trim();
    const createBtn = document.querySelector('#addTaskModal .create-btn');

    if (!title) {
        createBtn.disabled = true;
        createBtn.style.opacity = '0.6';
        createBtn.style.cursor = 'not-allowed';
    } else {
        createBtn.disabled = false;
        createBtn.style.opacity = '1';
        createBtn.style.cursor = 'pointer';
    }
}

function closeAddTaskModal() {
    const modal = document.getElementById('addTaskModal');
    modal.classList.remove('active');
    document.body.style.overflow = '';
    closeAllAddTaskDropdowns();
}

function resetAddTaskForm() {
    // Clear inputs
    document.getElementById('addTaskTitle').value = '';
    document.getElementById('addTaskDescription').value = '';

    // Clear the date input
    const dueDateInput = document.getElementById('addTaskDueDate');
    if (dueDateInput) {
        dueDateInput.value = '';
        updateClearButtonVisibility(dueDateInput);
    }

    // Clear attachments
    newTaskAttachments = [];
    document.getElementById('addTaskAttachmentsDropdownList').innerHTML = '<div class="no-results">No attachments</div>';
    updateAddTaskAttachmentsDisplay();

    // Reset data
    newTaskData = {
        title: '',
        description: '',
        priority: '1',
        status: 1,
        dueDate: '',
        assigneeIds: []
    };

    // Update UI
    updateAddTaskUI();

    // Validate form
    validateAddTaskForm();

    const titleInput = document.getElementById('addTaskTitle');
    titleInput.removeEventListener('input', validateAddTaskForm);
    titleInput.addEventListener('input', validateAddTaskForm);
}

function updateAddTaskUI() {
    // Update priority buttons
    document.querySelectorAll('#addTaskModal .priority-btn').forEach(btn => {
        btn.classList.remove('active');
    });
    const activePriorityBtn = document.querySelector(`#addTaskModal .priority-btn[data-priority="${newTaskData.priority}"]`);
    if (activePriorityBtn) {
        activePriorityBtn.classList.add('active');
    }

    // Update status button text
    const statusBtn = document.querySelector('#addTaskModal .status-btn');
    if (statusBtn) {
        const statusName = statusNames[newTaskData.status] || 'To Do';
        statusBtn.innerHTML = `<i class="bi bi-columns sidebar-icon"></i><span>Status: ${statusName}</span>`;
    }

    // Update due date display
    const dueDateInput = document.getElementById('addTaskDueDate');
    if (dueDateInput) {
        if (newTaskData.dueDate) {
            dueDateInput.value = newTaskData.dueDate;
            dueDateInput.placeholder = '';
        } else {
            dueDateInput.value = '';
            dueDateInput.placeholder = 'Due date';
        }
        updateClearButtonVisibility(dueDateInput);
    }

    // Update members button
    const membersBtn = document.querySelector('#addTaskModal .members-btn');
    if (membersBtn) {
        membersBtn.innerHTML = `<i class="bi bi-people sidebar-icon"></i><span>Members${newTaskData.assigneeIds.length ? ` (${newTaskData.assigneeIds.length})` : ''}</span>`;
    }

    // Update attachment button
    const attachmentBtn = document.querySelector('#addTaskModal .sidebar-btn[onclick*="addTaskAttachmentsDropdown"]');
    if (attachmentBtn) {
        attachmentBtn.innerHTML = `<i class="bi bi-paperclip sidebar-icon"></i><span>Attachment${newTaskAttachments.length ? ` (${newTaskAttachments.length})` : ''}</span>`;
    }
}

// ========== Add Task Date Picker Functions ==========

function openAddTaskDatePicker() {
    const dueDateInput = document.getElementById('addTaskDueDate');
    if (dueDateInput) {
        openDatePicker(dueDateInput);
    }
}

function clearAddTaskDueDate() {
    newTaskData.dueDate = '';
    updateAddTaskUI();
}

// Override the global selectDate function for add task modal
const originalSelectDate = window.selectDate;
window.selectDate = function (day) {
    if (!currentDatePicker || !currentDateInput) return;

    // Create the selected date using the current view month/year and the selected day
    const selectedDate = new Date(currentViewDate.getFullYear(), currentViewDate.getMonth(), day);

    // Format date as YYYY-MM-DD
    const year = selectedDate.getFullYear();
    const month = String(selectedDate.getMonth() + 1).padStart(2, '0');
    const dayFormatted = String(selectedDate.getDate()).padStart(2, '0');
    const formattedDate = `${year}-${month}-${dayFormatted}`;

    // Update the input value
    currentDateInput.value = formattedDate;
    updateClearButtonVisibility(currentDateInput);

    // Handle add task modal date selection
    if (currentDateInput.id === 'addTaskDueDate') {
        newTaskData.dueDate = formattedDate;
        updateAddTaskUI();
    }
    // Handle edit task modal date selection
    else if (currentDateInput.id === 'dueDateDisplayInput' && currentTask) {
        saveDueDateToServer(formattedDate);
    }
    // Handle filter dates
    else {
        applyFilters();
    }

    closeDatePicker();
};

// Priority handling
function setAddTaskPriority(priority, element) {
    newTaskData.priority = priority;
    updateAddTaskUI();
}

// Status handling
function setAddTaskStatus(status, element) {
    newTaskData.status = status;
    updateAddTaskUI();
    closeAllAddTaskDropdowns();
}

// Members handling
function toggleAddTaskMember(userId, userName, userItem) {
    if (newTaskData.assigneeIds.includes(userId)) {
        newTaskData.assigneeIds = newTaskData.assigneeIds.filter(id => id !== userId);
        userItem.classList.remove('selected');
    } else {
        // FIX: Verify the user is still a project member before adding
        if (usersData.some(user => user.Id === userId)) {
            newTaskData.assigneeIds.push(userId);
            userItem.classList.add('selected');
        } else {
            alert('This user is no longer a project member');
            return;
        }
    }
    updateAddTaskUI();
}

function populateAddTaskMembersList() {
    const membersList = document.getElementById('addTaskMembersList');
    if (!membersList) return;

    membersList.innerHTML = '';

    if (usersData.length === 0) {
        membersList.innerHTML = '<div class="no-results">No members found</div>';
        return;
    }

    usersData.forEach((user, index) => {
        const colorIndex = (index % 6) + 1;
        const userItem = document.createElement('div');
        userItem.className = 'user-item';
        userItem.setAttribute('data-user-id', user.Id);

        const isSelected = newTaskData.assigneeIds.includes(user.Id);

        if (isSelected) {
            userItem.classList.add('selected');
        }

        userItem.innerHTML = `
                <div class="user-avatar avatar-color-${colorIndex}">
                    ${getUserInitials(user.UserName)}
                </div>
                <span>${user.UserName || 'Unknown User'}</span>
            `;

        userItem.onclick = () => toggleAddTaskMember(user.Id, user.UserName, userItem);
        membersList.appendChild(userItem);
    });
}

function filterAddTaskMembers() {
    const searchTerm = document.getElementById('addTaskMembersSearch').value.toLowerCase();
    const userItems = document.querySelectorAll('#addTaskMembersList .user-item');

    userItems.forEach(item => {
        const userName = item.querySelector('span').textContent.toLowerCase();
        if (userName.includes(searchTerm)) {
            item.style.display = 'flex';
        } else {
            item.style.display = 'none';
        }
    });
}

// Dropdown Management
function toggleAddTaskDropdown(dropdownId, button) {
    const dropdown = document.getElementById(dropdownId);
    const allDropdowns = document.querySelectorAll('.sidebar-dropdown');

    allDropdowns.forEach(d => {
        if (d.id !== dropdownId) d.classList.remove('active');
    });

    if (dropdown.classList.contains('active')) {
        dropdown.classList.remove('active');
    } else {
        dropdown.classList.add('active');
        positionDropdown(dropdown, button);

        // Initialize dropdown content
        if (dropdownId === 'addTaskMembersDropdown') {
            populateAddTaskMembersList();
        } else if (dropdownId === 'addTaskStatusDropdown') {
            // Update status dropdown selection
            document.querySelectorAll('#addTaskStatusDropdown .status-option').forEach(option => {
                const status = parseInt(option.getAttribute('data-status'));
                option.classList.toggle('selected', status === newTaskData.status);
            });
        }
    }
}

function closeAllAddTaskDropdowns() {
    document.querySelectorAll('.sidebar-dropdown').forEach(d => {
        d.classList.remove('active');
    });
}

function positionDropdown(dropdown, button) {
    const buttonRect = button.getBoundingClientRect();
    const dropdownRect = dropdown.getBoundingClientRect();
    const viewportWidth = window.innerWidth;
    const viewportHeight = window.innerHeight;

    let left = buttonRect.right + 8;
    let top = buttonRect.top;

    if (left + dropdownRect.width > viewportWidth - 16) {
        left = buttonRect.left - dropdownRect.width - 8;
    }

    if (top + dropdownRect.height > viewportHeight - 16) {
        top = viewportHeight - dropdownRect.height - 16;
    }

    if (top < 16) {
        top = 16;
    }

    dropdown.style.position = 'fixed';
    dropdown.style.left = left + 'px';
    dropdown.style.top = top + 'px';
    dropdown.style.zIndex = '10002';
}

// Create task
async function createTaskFromModal() {
    const title = document.getElementById('addTaskTitle').value.trim();

    if (!title) {
        alert('Please enter a task title');
        document.getElementById('addTaskTitle').focus();
        return;
    }

    // Double-check validation
    validateAddTaskForm();
    if (document.querySelector('#addTaskModal .create-btn').disabled) {
        return;
    }

    newTaskData.title = title;
    newTaskData.description = document.getElementById('addTaskDescription').value.trim();

    try {
        const btn = document.querySelector('#addTaskModal .create-btn');
        const originalText = btn.innerHTML;
        btn.innerHTML = '<i class="bi bi-hourglass-split sidebar-icon"></i><span>Creating...</span>';
        btn.disabled = true;

        const taskData = await createTaskOnServer(newTaskData);

        if (taskData && taskData.Id) {
            // FIX: Ensure taskUsers is properly set
            if (!taskData.taskUsers) {
                taskData.taskUsers = usersData.filter(user => newTaskData.assigneeIds.includes(user.Id));
            }

            taskData.taskAttachments = newTaskAttachments;

            // Add to tasksData
            tasksData.push(taskData);

            // Refresh the table view
            filteredTasks = [...tasksData];
            applySorting();
            applyFilters();

            currentPage = 1;
            renderTable();
            updatePagination();

            closeAddTaskModal();

            console.log('Task created successfully with members:', taskData.taskUsers);
        }
    } catch (error) {
        console.error('Error creating task:', error);
        alert('Error creating task. Please try again.');
    } finally {
        // Reset button
        const btn = document.querySelector('#addTaskModal .create-btn');
        btn.innerHTML = '<i class="bi bi-check-lg sidebar-icon"></i><span>Create Task</span>';
        btn.disabled = false;
        validateAddTaskForm();
    }
}

async function createTaskOnServer(taskData) {
    const projectId = currentProjectId;
    const formData = new FormData();

    // Add basic task data
    formData.append('projectId', projectId);
    formData.append('title', taskData.title);
    formData.append('description', taskData.description);
    formData.append('dueDate', taskData.dueDate);
    formData.append('priorityStatus', taskData.priority);
    formData.append('progressStatus', taskData.status.toString());

    // Add assignee IDs
    taskData.assigneeIds.forEach(userId => {
        formData.append('assigneeIds', userId);
    });

    // Add attachments
    newTaskAttachments.forEach(attachment => {
        if (attachment.File instanceof File) {
            formData.append('attachments', attachment.File);
        }
    });

    try {
        const response = await fetch('/Project/CreateTask', {
            method: 'POST',
            body: formData
        });

        const data = await response.json();

        if (data.success) {
            // Get the assigned users from usersData
            const assignedUsers = usersData.filter(user =>
                taskData.assigneeIds.includes(user.Id)
            ).map(user => ({
                Id: user.Id,
                UserName: user.UserName,
                Email: user.Email
            }));

            const newTask = {
                Id: data.task?.id || data.task?.Id || 'temp_' + Date.now(),
                Title: data.task?.title || data.task?.Title || taskData.title,
                Description: data.task?.description || data.task?.Description || taskData.description,
                DueDate: data.task?.dueDate || data.task?.DueDate || taskData.dueDate,
                PriorityStatus: data.task?.priorityStatus || data.task?.PriorityStatus || taskData.priority,
                ProgressStatus: data.task?.progressStatus || data.task?.ProgressStatus || taskData.status,
                CreatedDateTime: data.task?.createdDateTime || data.task?.CreatedDateTime || new Date().toISOString(),
                CreatedBy: data.task?.createdBy || data.task?.CreatedBy || currentUserId,
                AssignedByUserName: data.task?.assignedByUserName || 'You',
                taskUsers: assignedUsers, // FIX: Properly populate taskUsers
                taskAttachments: [],
                taskComments: [],
                CommentsCount: 0
            };

            console.log('Created task with members:', newTask);
            return newTask;
        } else {
            throw new Error(data.message || 'Failed to create task');
        }
    } catch (error) {
        console.error('Error creating task:', error);
        throw error;
    }
}
// ========== Add Task Attachments ==========

let newTaskAttachments = [];

function setupAddTaskFileUpload() {
    const uploadArea = document.getElementById('addTaskFileUploadArea');

    uploadArea.addEventListener('dragover', (e) => {
        e.preventDefault();
        uploadArea.classList.add('dragover');
    });

    uploadArea.addEventListener('dragleave', () => {
        uploadArea.classList.remove('dragover');
    });

    uploadArea.addEventListener('drop', (e) => {
        e.preventDefault();
        uploadArea.classList.remove('dragover');

        const files = Array.from(e.dataTransfer.files);
        if (files.length > 0) {
            handleAddTaskFileUpload(files);
        }
    });
}

function handleAddTaskFileInputChange(files) {
    const fileArray = Array.from(files);
    if (fileArray.length > 0) {
        handleAddTaskFileUpload(fileArray);
        document.getElementById('addTaskFileInput').value = '';
    }
}

async function handleAddTaskFileUpload(files) {
    if (!files.length) return;

    let successCount = 0;
    for (const file of files) {
        const attachment = {
            Id: 'temp_' + Date.now() + '_' + successCount,
            FileName: file.name,
            File: file, // Store the File object
            FilePath: URL.createObjectURL(file), // Create preview URL
            FileSize: file.size,
            CreatedDateTime: new Date().toISOString()
        };

        newTaskAttachments.push(attachment);
        addAttachmentToAddTaskList(attachment);
        successCount++;
    }

    if (successCount > 0) {
        updateAddTaskAttachmentsDisplay();
        console.log(`Successfully added ${successCount} files`);
    }
}

function addAttachmentToAddTaskList(attachment) {
    const attachmentsList = document.getElementById('addTaskAttachmentsDropdownList');

    const noResults = attachmentsList.querySelector('.no-results');
    if (noResults) {
        noResults.remove();
    }

    const fileIcon = getFileIcon(attachment.FileName);
    const attachmentElement = document.createElement('div');
    attachmentElement.className = 'attachment-preview';
    attachmentElement.innerHTML = `
        <i class="bi ${fileIcon} attachment-icon"></i>
        <div class="attachment-info">
            <span class="attachment-name">${attachment.FileName}</span>
            <span class="file-size">${formatDate(attachment.CreatedDateTime)}</span>
        </div>
        <div class="attachment-actions">
            <button class="action-btn" onclick="downloadAddTaskAttachment('${attachment.Id}')" title="Download">
                <i class="bi bi-download"></i>
            </button>
            <button class="action-btn" onclick="removeAddTaskAttachment('${attachment.Id}')" title="Delete">
                <i class="bi bi-trash"></i>
            </button>
        </div>
    `;
    attachmentsList.appendChild(attachmentElement);
}

function updateAddTaskAttachmentsDisplay() {
    const attachmentsSection = document.getElementById('addTaskAttachmentsSection');
    const attachmentsList = document.getElementById('addTaskAttachmentsList');

    if (newTaskAttachments.length === 0) {
        attachmentsSection.style.display = 'none';
        attachmentsList.innerHTML = '';
        return;
    }

    attachmentsSection.style.display = 'block';
    attachmentsList.innerHTML = '';

    newTaskAttachments.forEach(attachment => {
        addAttachmentToAddTaskModalList(attachment);
    });

    // Update attachment button text
    const attachmentBtn = document.querySelector('#addTaskModal .sidebar-btn[onclick*="addTaskAttachmentsDropdown"]');
    if (attachmentBtn) {
        attachmentBtn.innerHTML = `<i class="bi bi-paperclip sidebar-icon"></i><span>Attachment (${newTaskAttachments.length})</span>`;
    }
}

function addAttachmentToAddTaskModalList(attachment) {
    const attachmentsList = document.getElementById('addTaskAttachmentsList');

    const fileIcon = getFileIcon(attachment.FileName);
    const uploadDate = formatDate(attachment.CreatedDateTime);

    const attachmentElement = document.createElement('div');
    attachmentElement.className = 'attachment-item';
    attachmentElement.innerHTML = `
        <div class="attachment-icon-large">
            <i class="bi ${fileIcon}"></i>
        </div>
        <div class="attachment-details">
            <span class="attachment-name">${attachment.FileName}</span>
            <div class="attachment-meta">
                <span>Added ${uploadDate}</span>
            </div>
        </div>
        <div class="attachment-actions-modal">
            <button class="action-btn-modal" onclick="downloadAddTaskAttachment('${attachment.Id}')" title="Download">
                <i class="bi bi-download"></i>
                Download
            </button>
            <button class="action-btn-modal" onclick="removeAddTaskAttachment('${attachment.Id}')" title="Delete">
                <i class="bi bi-trash"></i>
                Delete
            </button>
        </div>
    `;

    attachmentsList.appendChild(attachmentElement);
}

function downloadAddTaskAttachment(attachmentId) {
    const attachment = newTaskAttachments.find(a => a.Id === attachmentId);
    if (attachment && attachment.FilePath) {
        try {
            // For new attachments, we might need to create a blob URL
            // This is a simplified version - you might need to adjust based on your file handling
            window.open(attachment.FilePath, '_blank');
        } catch (error) {
            console.error('Error downloading attachment:', error);
            window.open(attachment.FilePath, '_blank');
        }
    }
}

function removeAddTaskAttachment(attachmentId) {
    // Remove from array
    newTaskAttachments = newTaskAttachments.filter(a => a.Id !== attachmentId);

    // Update UI
    updateAddTaskAttachmentsDisplay();

    // Remove from dropdown list
    const attachmentElement = document.querySelector(`#addTaskAttachmentsDropdownList .attachment-preview [onclick*="${attachmentId}"]`);
    if (attachmentElement) {
        attachmentElement.closest('.attachment-preview').remove();
    }

    // Show no results message if no attachments left
    const attachmentsList = document.getElementById('addTaskAttachmentsDropdownList');
    if (attachmentsList.children.length === 0) {
        attachmentsList.innerHTML = '<div class="no-results">No attachments</div>';
    }
}