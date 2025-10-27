// Date picker functionality
let currentDateInput = null;
let currentDatePicker = null;
let currentViewDate = new Date();

function initCustomDatePickers() {
    const dateInputs = document.querySelectorAll('.date-text-input');

    dateInputs.forEach(input => {
        input.addEventListener('click', function (e) {
            e.stopPropagation();

            // Check if clicking the same input that already has an open picker
            if (currentDateInput === this && currentDatePicker) {
                closeDatePicker();
            } else {
                openDatePicker(this);
            }
        });

        // Also validate on manual input
        input.addEventListener('blur', function () {
            validateDateRange();
        });

        // Initialize clear button visibility
        updateClearButtonVisibility(input);
    });

    // Close date picker when clicking outside
    document.addEventListener('click', function () {
        closeDatePicker();
    });
}

function openDatePicker(input) {
    // Close any existing date picker first
    closeDatePicker();

    currentDateInput = input;

    // Use the input value or current date for the view
    const inputValue = input.value;
    if (inputValue) {
        const [year, month, day] = inputValue.split('-').map(Number);
        currentViewDate = new Date(year, month - 1, day); // month is 0-indexed in Date
    } else {
        currentViewDate = new Date();
    }

    // Create date picker
    const datePicker = document.createElement('div');
    datePicker.className = 'date-picker-modal';
    datePicker.id = 'datePickerModal';

    datePicker.innerHTML = createDatePickerHTML(currentViewDate);
    document.body.appendChild(datePicker);

    // Position the date picker
    const rect = input.getBoundingClientRect();
    datePicker.style.position = 'fixed';
    datePicker.style.left = rect.left + 'px';
    datePicker.style.top = (rect.bottom + 8) + 'px';
    datePicker.style.display = 'block';

    currentDatePicker = datePicker;

    // Add event listeners
    attachDatePickerEvents();
}

function closeDatePicker() {
    if (currentDatePicker) {
        currentDatePicker.remove();
        currentDatePicker = null;
        currentDateInput = null;
    }
}

function createDatePickerHTML(viewDate) {
    const year = viewDate.getFullYear();
    const month = viewDate.getMonth();
    const today = new Date();

    // Get first day of month and number of days
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const daysInMonth = lastDay.getDate();
    const startingDay = firstDay.getDay(); // 0 = Sunday, 1 = Monday, etc.

    const monthNames = ['January', 'February', 'March', 'April', 'May', 'June',
        'July', 'August', 'September', 'October', 'November', 'December'];

    const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];

    let html = `
        <div class="date-picker-header">
            <button class="date-picker-nav prev-month" type="button">‹</button>
            <div class="date-picker-title">${monthNames[month]} ${year}</div>
            <button class="date-picker-nav next-month" type="button">›</button>
        </div>
        <div class="date-picker-grid">
    `;

    // Day names
    dayNames.forEach(day => {
        html += `<div class="date-picker-day">${day}</div>`;
    });

    // Previous month days
    const prevMonthLastDay = new Date(year, month, 0).getDate();
    for (let i = 0; i < startingDay; i++) {
        const day = prevMonthLastDay - startingDay + i + 1;
        const prevMonthDate = new Date(year, month - 1, day);
        const isDisabled = isDateDisabled(prevMonthDate);
        const disabledClass = isDisabled ? 'disabled' : '';
        html += `<div class="date-picker-date other-month ${disabledClass}">${day}</div>`;
    }

    // Current month days
    for (let day = 1; day <= daysInMonth; day++) {
        const date = new Date(year, month, day);
        const isToday = date.toDateString() === today.toDateString();
        const isDisabled = isDateDisabled(date);

        // Check if this date is selected
        let isSelected = false;
        if (currentDateInput && currentDateInput.value) {
            const selectedDate = new Date(currentDateInput.value);
            isSelected = date.toDateString() === selectedDate.toDateString();
        }

        const todayClass = isToday ? 'today' : '';
        const selectedClass = isSelected ? 'selected' : '';
        const disabledClass = isDisabled ? 'disabled' : '';
        html += `<div class="date-picker-date ${todayClass} ${selectedClass} ${disabledClass}" data-day="${day}" ${isDisabled ? 'style="cursor: not-allowed; opacity: 0.5;"' : ''}>${day}</div>`;
    }

    // Next month days
    const totalCells = 42; // 6 rows * 7 days
    const cellsUsed = startingDay + daysInMonth;
    const remainingCells = totalCells - cellsUsed;

    for (let day = 1; day <= remainingCells; day++) {
        const nextMonthDate = new Date(year, month + 1, day);
        const isDisabled = isDateDisabled(nextMonthDate);
        const disabledClass = isDisabled ? 'disabled' : '';
        html += `<div class="date-picker-date other-month ${disabledClass}">${day}</div>`;
    }

    html += '</div>';

    // Add clear button at the bottom if the input has a value
    if (currentDateInput && currentDateInput.value) {
        html += `
            <div class="date-picker-footer">
                <button class="date-picker-clear-btn" type="button">
                    Clear
                </button>
            </div>
        `;
    }

    return html;
}

function isDateDisabled(date) {
    const startDateInput = document.getElementById('startDateFilter');
    const endDateInput = document.getElementById('endDateFilter');

    // If we're selecting start date and end date is already set
    if (currentDateInput && currentDateInput.id === 'startDateFilter' && endDateInput.value) {
        const endDate = new Date(endDateInput.value);
        return date > endDate;
    }

    // If we're selecting end date and start date is already set
    if (currentDateInput && currentDateInput.id === 'endDateFilter' && startDateInput.value) {
        const startDate = new Date(startDateInput.value);
        return date < startDate;
    }

    return false;
}

function attachDatePickerEvents() {
    if (!currentDatePicker) return;

    // Navigation buttons
    currentDatePicker.querySelector('.prev-month').addEventListener('click', function (e) {
        e.stopPropagation();
        navigateMonth(-1);
    });

    currentDatePicker.querySelector('.next-month').addEventListener('click', function (e) {
        e.stopPropagation();
        navigateMonth(1);
    });

    // Date selection
    const dateElements = currentDatePicker.querySelectorAll('.date-picker-date:not(.other-month):not(.disabled)');
    dateElements.forEach(dateEl => {
        dateEl.addEventListener('click', function (e) {
            e.stopPropagation();
            const day = parseInt(this.getAttribute('data-day'));
            selectDate(day);
        });
    });

    // Clear button in date picker
    const clearBtn = currentDatePicker.querySelector('.date-picker-clear-btn');
    if (clearBtn) {
        clearBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            clearDateFromPicker();
        });
    }

    // Disabled dates - show tooltip on hover
    const disabledElements = currentDatePicker.querySelectorAll('.date-picker-date.disabled');
    disabledElements.forEach(dateEl => {
        dateEl.addEventListener('mouseenter', function (e) {
            showDisabledTooltip(this, e);
        });

        dateEl.addEventListener('mouseleave', function () {
            hideDisabledTooltip();
        });
    });
}

function clearDateFromPicker() {
    if (!currentDateInput) return;

    // Clear the input value
    currentDateInput.value = '';
    updateClearButtonVisibility(currentDateInput);

    // Handle different contexts
    if (currentDateInput.id === 'addTaskDueDate') {
        // For add task modal - update the newTaskData
        if (typeof newTaskData !== 'undefined') {
            newTaskData.dueDate = '';
            if (typeof updateAddTaskUI !== 'undefined') {
                updateAddTaskUI();
            }
        }
    } else if (currentDateInput.id === 'dueDateDisplayInput') {
        // For edit task modal - save to server
        if (typeof saveDueDateToServer !== 'undefined' && currentTask) {
            saveDueDateToServer('');
        }
    } else {
        // For filter dates
        applyFilters();
    }

    closeDatePicker();
}

function showDisabledTooltip(element, event) {
    const startDateInput = document.getElementById('startDateFilter');
    const endDateInput = document.getElementById('endDateFilter');

    let tooltipText = '';

    if (currentDateInput.id === 'startDateFilter' && endDateInput.value) {
        tooltipText = `Start date cannot be after ${new Date(endDateInput.value).toLocaleDateString()}`;
    } else if (currentDateInput.id === 'endDateFilter' && startDateInput.value) {
        tooltipText = `End date cannot be before ${new Date(startDateInput.value).toLocaleDateString()}`;
    }

    if (tooltipText) {
        const tooltip = document.createElement('div');
        tooltip.className = 'date-tooltip';
        tooltip.textContent = tooltipText;
        tooltip.style.cssText = `
            position: fixed;
            background: #2b2f36;
            color: #e9ecef;
            padding: 8px 12px;
            border-radius: 4px;
            font-size: 12px;
            z-index: 10004;
            border: 1px solid #444;
            box-shadow: 0 2px 8px rgba(0,0,0,0.3);
            left: ${event.clientX + 10}px;
            top: ${event.clientY + 10}px;
            white-space: nowrap;
        `;
        document.body.appendChild(tooltip);
        element._tooltip = tooltip;
    }
}

function hideDisabledTooltip() {
    const tooltips = document.querySelectorAll('.date-tooltip');
    tooltips.forEach(tooltip => tooltip.remove());
}

function navigateMonth(direction) {
    if (!currentDatePicker || !currentDateInput) return;

    // Update the view date
    currentViewDate = new Date(currentViewDate.getFullYear(), currentViewDate.getMonth() + direction, 1);

    // Recreate the date picker with new month
    currentDatePicker.innerHTML = createDatePickerHTML(currentViewDate);

    // Reattach event listeners
    attachDatePickerEvents();
}

function selectDate(day) {
    if (!currentDatePicker || !currentDateInput) return;

    // Create the selected date using UTC to avoid timezone issues
    const selectedDate = new Date(Date.UTC(
        currentViewDate.getFullYear(),
        currentViewDate.getMonth(),
        day
    ));

    // Validate the date range
    if (!validateSelectedDate(selectedDate)) {
        return;
    }

    // Format date as YYYY-MM-DD using UTC
    const year = selectedDate.getUTCFullYear();
    const month = String(selectedDate.getUTCMonth() + 1).padStart(2, '0');
    const dayFormatted = String(selectedDate.getUTCDate()).padStart(2, '0');
    const formattedDate = `${year}-${month}-${dayFormatted}`;

    // Update the input value
    currentDateInput.value = formattedDate;
    updateClearButtonVisibility(currentDateInput);

    // Handle different contexts
    if (currentDateInput.id === 'addTaskDueDate') {
        // For add task modal - update the newTaskData
        if (typeof newTaskData !== 'undefined') {
            newTaskData.dueDate = formattedDate;
            if (typeof updateAddTaskUI !== 'undefined') {
                updateAddTaskUI();
            }
        }
    } else if (currentDateInput.id === 'dueDateDisplayInput') {
        // For edit task modal - save to server
        if (typeof saveDueDateToServer !== 'undefined' && currentTask) {
            saveDueDateToServer(formattedDate);
        }
    } else {
        // For filter dates
        applyFilters();
    }

    closeDatePicker();
}

function validateSelectedDate(selectedDate) {
    const startDateInput = document.getElementById('startDateFilter');
    const endDateInput = document.getElementById('endDateFilter');

    // If selecting start date and end date exists
    if (currentDateInput.id === 'startDateFilter' && endDateInput.value) {
        const endDate = new Date(endDateInput.value);
        if (selectedDate > endDate) {
            alert(`Start date cannot be after end date (${endDate.toLocaleDateString()})`);
            return false;
        }
    }

    // If selecting end date and start date exists
    if (currentDateInput.id === 'endDateFilter' && startDateInput.value) {
        const startDate = new Date(startDateInput.value);
        if (selectedDate < startDate) {
            alert(`End date cannot be before start date (${startDate.toLocaleDateString()})`);
            return false;
        }
    }

    return true;
}

function validateDateRange() {
    const startDateInput = document.getElementById('startDateFilter');
    const endDateInput = document.getElementById('endDateFilter');

    if (startDateInput.value && endDateInput.value) {
        const startDate = new Date(startDateInput.value);
        const endDate = new Date(endDateInput.value);

        if (startDate > endDate) {
            alert('Start date cannot be after end date');
            // Auto-correct by swapping dates
            const temp = startDateInput.value;
            startDateInput.value = endDateInput.value;
            endDateInput.value = temp;

            // Re-apply filters with corrected dates
            applyFilters();
        }
    }
}

function clearDate(inputId) {
    const input = document.getElementById(inputId);
    if (input) {
        input.value = '';
        updateClearButtonVisibility(input);

        // Handle different contexts
        if (inputId === 'addTaskDueDate') {
            // For add task modal
            if (typeof newTaskData !== 'undefined') {
                newTaskData.dueDate = '';
                if (typeof updateAddTaskUI !== 'undefined') {
                    updateAddTaskUI();
                }
            }
        } else if (inputId === 'dueDateDisplayInput') {
            // For edit task modal
            if (typeof saveDueDateToServer !== 'undefined' && currentTask) {
                saveDueDateToServer('');
            }
        } else {
            // For filter dates
            applyFilters();
        }

        // If we're in the date picker, close it
        if (currentDateInput && currentDateInput.id === inputId) {
            closeDatePicker();
        }
    }
}

function updateClearButtonVisibility(input) {
    const wrapper = input.closest('.date-input-wrapper');
    if (!wrapper) return;

    const clearBtn = wrapper.querySelector('.date-clear-btn');
    if (!clearBtn) return;

    if (input.value) {
        clearBtn.classList.add('visible');
    } else {
        clearBtn.classList.remove('visible');
    }
}

function clearAllDates() {
    clearDate('startDateFilter');
    clearDate('endDateFilter');
}

// Initialize when page loads
document.addEventListener('DOMContentLoaded', function () {
    initCustomDatePickers();
});