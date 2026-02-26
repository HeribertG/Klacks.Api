// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class UiControlSeedService
{
    private readonly IUiControlRepository _uiControlRepository;
    private readonly ILogger<UiControlSeedService> _logger;

    public UiControlSeedService(
        IUiControlRepository uiControlRepository,
        ILogger<UiControlSeedService> logger)
    {
        _uiControlRepository = uiControlRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var controls = BuildDefaultControls();
        var upserted = 0;

        foreach (var control in controls)
        {
            await _uiControlRepository.UpsertAsync(control, cancellationToken);
            upserted++;
        }

        _logger.LogInformation("Upserted {Count} UI controls.", upserted);
    }

    private static List<UiControl> BuildDefaultControls()
    {
        var controls = new List<UiControl>();

        controls.AddRange(BuildMainNavControls());
        controls.AddRange(BuildSettingsOwnerAddressControls());
        controls.AddRange(BuildSettingsGeneralControls());
        controls.AddRange(BuildModalControls());
        controls.AddRange(BuildSaveBarControls());
        controls.AddRange(BuildLlmChatControls());
        controls.AddRange(BuildClientControls());
        controls.AddRange(BuildSettingsBranchesControls());

        return controls;
    }

    private static List<UiControl> BuildMainNavControls()
    {
        return
        [
            Create(UiPageKeys.MainNav, "dashboard", "header-logo-icon",
                UiControlDefaults.ControlTypeButton, "Dashboard", "/workplace/dashboard", 0),

            Create(UiPageKeys.MainNav, "absences", "open-absences",
                UiControlDefaults.ControlTypeButton, "Absences", "/workplace/absence", 1),

            Create(UiPageKeys.MainNav, "groups", "open-groups",
                UiControlDefaults.ControlTypeButton, "Groups", "/workplace/group", 2),

            Create(UiPageKeys.MainNav, "shifts", "open-shifts",
                UiControlDefaults.ControlTypeButton, "Shifts", "/workplace/shift", 3),

            Create(UiPageKeys.MainNav, "schedules", "open-schedules",
                UiControlDefaults.ControlTypeButton, "Schedules", "/workplace/schedule", 4),

            Create(UiPageKeys.MainNav, "employees", "open-employees",
                UiControlDefaults.ControlTypeButton, "Employees", "/workplace/client", 5),

            Create(UiPageKeys.MainNav, "profile", "open-profile",
                UiControlDefaults.ControlTypeButton, "Profile", "/workplace/profile", 6),

            Create(UiPageKeys.MainNav, "settings", "open-settings",
                UiControlDefaults.ControlTypeButton, "Settings", "/workplace/settings", 7)
        ];
    }

    private static List<UiControl> BuildSettingsOwnerAddressControls()
    {
        return
        [
            Create(UiPageKeys.SettingsOwnerAddress, "section", "settings-owner-address",
                UiControlDefaults.ControlTypeSection, "Owner Address Section", "/workplace/settings", 0),

            Create(UiPageKeys.SettingsOwnerAddress, "form", "owner-address-form",
                UiControlDefaults.ControlTypeForm, "Owner Address Form", "/workplace/settings", 1),

            Create(UiPageKeys.SettingsOwnerAddress, "name", "setting-owner-address-name",
                UiControlDefaults.ControlTypeInput, "Company Name", "/workplace/settings", 2),

            Create(UiPageKeys.SettingsOwnerAddress, "tel", "setting-owner-address-tel",
                UiControlDefaults.ControlTypeInput, "Phone", "/workplace/settings", 3),

            Create(UiPageKeys.SettingsOwnerAddress, "supplement", "setting-owner-address-supplement",
                UiControlDefaults.ControlTypeInput, "Supplement Address", "/workplace/settings", 4),

            Create(UiPageKeys.SettingsOwnerAddress, "email", "setting-owner-address-email",
                UiControlDefaults.ControlTypeInput, "Email", "/workplace/settings", 5),

            Create(UiPageKeys.SettingsOwnerAddress, "street", "setting-owner-address-street",
                UiControlDefaults.ControlTypeInput, "Street", "/workplace/settings", 6),

            Create(UiPageKeys.SettingsOwnerAddress, "zip", "setting-owner-address-zip",
                UiControlDefaults.ControlTypeInput, "ZIP Code", "/workplace/settings", 7),

            Create(UiPageKeys.SettingsOwnerAddress, "city", "setting-owner-address-city",
                UiControlDefaults.ControlTypeInput, "City", "/workplace/settings", 8),

            Create(UiPageKeys.SettingsOwnerAddress, "country", "setting-owner-address-country",
                UiControlDefaults.ControlTypeSelect, "Country", "/workplace/settings", 9),

            Create(UiPageKeys.SettingsOwnerAddress, "state", "setting-owner-address-state",
                UiControlDefaults.ControlTypeSelect, "State/Canton", "/workplace/settings", 10)
        ];
    }

    private static List<UiControl> BuildSettingsGeneralControls()
    {
        return
        [
            Create(UiPageKeys.SettingsGeneral, "name", "setting-general-name",
                UiControlDefaults.ControlTypeInput, "App Name", "/workplace/settings", 0),

            Create(UiPageKeys.SettingsGeneral, "delete-icon-btn", "setting-general-delete-icon-btn",
                UiControlDefaults.ControlTypeButton, "Delete Icon", "/workplace/settings", 1),

            Create(UiPageKeys.SettingsGeneral, "icon-file-input", "setting-general-icon-file-input",
                UiControlDefaults.ControlTypeFileInput, "Icon Upload", "/workplace/settings", 2),

            Create(UiPageKeys.SettingsGeneral, "icon-upload-area", "setting-general-icon-upload-area",
                UiControlDefaults.ControlTypeContainer, "Icon Upload Area", "/workplace/settings", 3),

            Create(UiPageKeys.SettingsGeneral, "delete-logo-btn", "setting-general-delete-logo-btn",
                UiControlDefaults.ControlTypeButton, "Delete Logo", "/workplace/settings", 4),

            Create(UiPageKeys.SettingsGeneral, "logo-file-input", "setting-general-logo-file-input",
                UiControlDefaults.ControlTypeFileInput, "Logo Upload", "/workplace/settings", 5),

            Create(UiPageKeys.SettingsGeneral, "logo-upload-area", "setting-general-logo-upload-area",
                UiControlDefaults.ControlTypeContainer, "Logo Upload Area", "/workplace/settings", 6)
        ];
    }

    private static List<UiControl> BuildModalControls()
    {
        return
        [
            Create(UiPageKeys.Modal, "delete-cancel", "modal-delete-cancel",
                UiControlDefaults.ControlTypeButton, "Cancel Delete", null, 0),

            Create(UiPageKeys.Modal, "delete-confirm", "modal-delete-confirm",
                UiControlDefaults.ControlTypeButton, "Confirm Delete", null, 1),

            Create(UiPageKeys.Modal, "input-text", "modal-input-text",
                UiControlDefaults.ControlTypeInput, "Input Text", null, 2),

            Create(UiPageKeys.Modal, "input-cancel", "modal-input-cancel",
                UiControlDefaults.ControlTypeButton, "Cancel Input", null, 3),

            Create(UiPageKeys.Modal, "input-confirm", "modal-input-confirm",
                UiControlDefaults.ControlTypeButton, "Confirm Input", null, 4),

            Create(UiPageKeys.Modal, "message-cancel", "modal-message-cancel",
                UiControlDefaults.ControlTypeButton, "Cancel Message", null, 5),

            Create(UiPageKeys.Modal, "message-confirm", "modal-message-confirm",
                UiControlDefaults.ControlTypeButton, "Confirm Message", null, 6)
        ];
    }

    private static List<UiControl> BuildSaveBarControls()
    {
        return
        [
            Create(UiPageKeys.SaveBar, "save", "shift-save-btn",
                UiControlDefaults.ControlTypeButton, "Save", null, 0),

            Create(UiPageKeys.SaveBar, "save-and-close", "shift-save-and-close-btn",
                UiControlDefaults.ControlTypeButton, "Save and Close", null, 1)
        ];
    }

    private static List<UiControl> BuildLlmChatControls()
    {
        return
        [
            Create(UiPageKeys.LlmChat, "toggle-btn", "header-assistant-button",
                UiControlDefaults.ControlTypeButton, "Open/Close Chat", null, 0),

            Create(UiPageKeys.LlmChat, "input", "assistant-chat-input",
                UiControlDefaults.ControlTypeInput, "Chat Input", null, 1),

            Create(UiPageKeys.LlmChat, "send-btn", "assistant-chat-send-btn",
                UiControlDefaults.ControlTypeButton, "Send Message", null, 2),

            Create(UiPageKeys.LlmChat, "messages", "assistant-chat-messages",
                UiControlDefaults.ControlTypeContainer, "Chat Messages", null, 3),

            Create(UiPageKeys.LlmChat, "clear-btn", "assistant-chat-clear-btn",
                UiControlDefaults.ControlTypeButton, "Clear Chat", null, 4)
        ];
    }

    private static List<UiControl> BuildClientControls()
    {
        return
        [
            Create(UiPageKeys.Client, "new-btn", "new-address-button",
                UiControlDefaults.ControlTypeButton, "New Employee", "/workplace/client", 0),

            Create(UiPageKeys.Client, "firstname", "firstname",
                UiControlDefaults.ControlTypeInput, "First Name", "/workplace/client", 1),

            Create(UiPageKeys.Client, "lastname", "profile-name",
                UiControlDefaults.ControlTypeInput, "Last Name", "/workplace/client", 2),

            Create(UiPageKeys.Client, "gender", "gender",
                UiControlDefaults.ControlTypeSelect, "Gender", "/workplace/client", 3),

            Create(UiPageKeys.Client, "street", "street",
                UiControlDefaults.ControlTypeInput, "Street", "/workplace/client", 4),

            Create(UiPageKeys.Client, "zip", "zip",
                UiControlDefaults.ControlTypeInput, "ZIP Code", "/workplace/client", 5),

            Create(UiPageKeys.Client, "city", "city",
                UiControlDefaults.ControlTypeInput, "City", "/workplace/client", 6),

            Create(UiPageKeys.Client, "state", "state",
                UiControlDefaults.ControlTypeSelect, "State/Canton", "/workplace/client", 7),

            Create(UiPageKeys.Client, "country", "country",
                UiControlDefaults.ControlTypeSelect, "Country", "/workplace/client", 8),

            CreateDynamic(UiPageKeys.Client, "table-row", "[id^='client-row-']",
                UiControlDefaults.SelectorTypeCss, UiControlDefaults.ControlTypeContainer,
                "Employee Row", "/workplace/client", 9, "client-row-{index}")
        ];
    }

    private static List<UiControl> BuildSettingsBranchesControls()
    {
        return
        [
            Create(UiPageKeys.SettingsBranches, "section", "branches-card",
                UiControlDefaults.ControlTypeSection, "Branch Section", "/workplace/settings", 0),

            Create(UiPageKeys.SettingsBranches, "add-btn", "branches-add-btn",
                UiControlDefaults.ControlTypeButton, "Add Branch", "/workplace/settings", 1),

            Create(UiPageKeys.SettingsBranches, "modal-form", "branches-modal-form",
                UiControlDefaults.ControlTypeForm, "Branch Modal Form", "/workplace/settings", 2),

            Create(UiPageKeys.SettingsBranches, "modal-close-btn", "branches-modal-close-btn",
                UiControlDefaults.ControlTypeButton, "Close Modal", "/workplace/settings", 3),

            Create(UiPageKeys.SettingsBranches, "modal-cancel-btn", "branches-modal-cancel-btn",
                UiControlDefaults.ControlTypeButton, "Cancel", "/workplace/settings", 4),

            Create(UiPageKeys.SettingsBranches, "modal-save-btn", "branches-modal-save-btn",
                UiControlDefaults.ControlTypeButton, "Save Branch", "/workplace/settings", 5),

            Create(UiPageKeys.SettingsBranches, "modal-name", "branches-modal-name",
                UiControlDefaults.ControlTypeInput, "Branch Name", "/workplace/settings", 6),

            Create(UiPageKeys.SettingsBranches, "modal-address", "branches-modal-address",
                UiControlDefaults.ControlTypeInput, "Branch Address", "/workplace/settings", 7),

            Create(UiPageKeys.SettingsBranches, "modal-phone", "branches-modal-phone",
                UiControlDefaults.ControlTypeInput, "Branch Phone", "/workplace/settings", 8),

            Create(UiPageKeys.SettingsBranches, "modal-email", "branches-modal-email",
                UiControlDefaults.ControlTypeInput, "Branch Email", "/workplace/settings", 9),

            CreateDynamic(UiPageKeys.SettingsBranches, "row-name", "[id^='branches-row-name-']",
                UiControlDefaults.SelectorTypeCss, UiControlDefaults.ControlTypeInput,
                "Branch Row Name", "/workplace/settings", 10, "branches-row-name-{id}"),

            CreateDynamic(UiPageKeys.SettingsBranches, "row-delete", "[id^='branches-row-delete-']",
                UiControlDefaults.SelectorTypeCss, UiControlDefaults.ControlTypeButton,
                "Delete Branch", "/workplace/settings", 11, "branches-row-delete-{id}"),

            Create(UiPageKeys.SettingsBranches, "delete-confirm", "modal-delete-confirm",
                UiControlDefaults.ControlTypeButton, "Confirm Delete", "/workplace/settings", 12)
        ];
    }

    private static UiControl Create(
        string pageKey,
        string controlKey,
        string selector,
        string controlType,
        string? label,
        string? route,
        int sortOrder)
    {
        return new UiControl
        {
            PageKey = pageKey,
            ControlKey = controlKey,
            Selector = selector,
            SelectorType = UiControlDefaults.DefaultSelectorType,
            ControlType = controlType,
            Label = label,
            Route = route,
            SortOrder = sortOrder
        };
    }

    private static UiControl CreateDynamic(
        string pageKey,
        string controlKey,
        string selector,
        string selectorType,
        string controlType,
        string? label,
        string? route,
        int sortOrder,
        string selectorPattern)
    {
        return new UiControl
        {
            PageKey = pageKey,
            ControlKey = controlKey,
            Selector = selector,
            SelectorType = selectorType,
            ControlType = controlType,
            Label = label,
            Route = route,
            SortOrder = sortOrder,
            IsDynamic = true,
            SelectorPattern = selectorPattern
        };
    }
}
