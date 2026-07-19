#nullable enable

using System.Globalization;
using System.Resources;

namespace TFactor.Properties;

/// <summary>
/// Strongly-typed accessor for the strings in Strings.resx - every piece of text shown to the user lives there, not inline in XAML or code-behind. Hand-written rather than relying on Visual Studio's ResX custom-tool generator, so it compiles the same way whether the resx was last saved by VS or edited directly.
/// </summary>
public static class Strings
{
    /// <summary>
    /// The resource manager for the Strings.resx file.
    /// </summary>
    private static ResourceManager? _resourceManager;

    /// <summary>
    /// The resource manager backing this class, created lazily and cached on first use.
    /// </summary>
    private static ResourceManager ResourceManager => _resourceManager ??= new ResourceManager("TFactor.Properties.Strings", typeof(Strings).Assembly);

    /// <summary>
    /// Looks up a string by its resx name, returning the name itself (wrapped in brackets) if it's missing - so a typo shows up obviously in the UI instead of silently rendering blank text.
    /// </summary>
    /// <param name="name">The resx entry name to look up</param>
    private static string Get(string name) => ResourceManager.GetString(name, CultureInfo.CurrentUICulture) ?? $"[{name}]";

    #region Common

    public static string Common_AppTitle => Get(nameof(Common_AppTitle));
    public static string Common_IssuerLabel => Get(nameof(Common_IssuerLabel));
    public static string Common_LabelLabel => Get(nameof(Common_LabelLabel));
    public static string Common_CancelTooltip => Get(nameof(Common_CancelTooltip));
    public static string Common_SaveTooltip => Get(nameof(Common_SaveTooltip));
    public static string Common_TagLabel => Get(nameof(Common_TagLabel));
    public static string Common_UntaggedLabel => Get(nameof(Common_UntaggedLabel));

    #endregion

    #region MainWindow

    public static string MainWindow_AddAccountTooltip => Get(nameof(MainWindow_AddAccountTooltip));
    public static string MainWindow_ImportTooltip => Get(nameof(MainWindow_ImportTooltip));
    public static string MainWindow_EmptyMessage => Get(nameof(MainWindow_EmptyMessage));
    public static string MainWindow_CodeTooltip => Get(nameof(MainWindow_CodeTooltip));
    public static string MainWindow_EditTooltip => Get(nameof(MainWindow_EditTooltip));
    public static string MainWindow_ManageTagsTooltip => Get(nameof(MainWindow_ManageTagsTooltip));

    #endregion

    #region AddAccountWindow

    public static string AddAccountWindow_Title => Get(nameof(AddAccountWindow_Title));
    public static string AddAccountWindow_SecretKeyLabel => Get(nameof(AddAccountWindow_SecretKeyLabel));
    public static string AddAccountWindow_AlgorithmLabel => Get(nameof(AddAccountWindow_AlgorithmLabel));
    public static string AddAccountWindow_DigitsLabel => Get(nameof(AddAccountWindow_DigitsLabel));
    public static string AddAccountWindow_MissingSecretError => Get(nameof(AddAccountWindow_MissingSecretError));
    public static string AddAccountWindow_InvalidSecretError => Get(nameof(AddAccountWindow_InvalidSecretError));

    #endregion

    #region EditAccountWindow

    public static string EditAccountWindow_Title => Get(nameof(EditAccountWindow_Title));
    public static string EditAccountWindow_RemoveTooltip => Get(nameof(EditAccountWindow_RemoveTooltip));
    public static string EditAccountWindow_MissingIssuerOrLabelError => Get(nameof(EditAccountWindow_MissingIssuerOrLabelError));
    public static string EditAccountWindow_RemoveConfirmMessage => Get(nameof(EditAccountWindow_RemoveConfirmMessage));
    public static string EditAccountWindow_RemoveConfirmTitle => Get(nameof(EditAccountWindow_RemoveConfirmTitle));

    #endregion

    #region ImportWindow
    public static string ImportWindow_Title => Get(nameof(ImportWindow_Title));
    public static string ImportWindow_Instructions => Get(nameof(ImportWindow_Instructions));
    public static string ImportWindow_ChooseScreenshotTooltip => Get(nameof(ImportWindow_ChooseScreenshotTooltip));
    public static string ImportWindow_ChooseAnotherScreenshotTooltip => Get(nameof(ImportWindow_ChooseAnotherScreenshotTooltip));
    public static string ImportWindow_ImportSelectedTooltip => Get(nameof(ImportWindow_ImportSelectedTooltip));
    public static string ImportWindow_FileDialogFilter => Get(nameof(ImportWindow_FileDialogFilter));
    public static string ImportWindow_FileDialogTitle => Get(nameof(ImportWindow_FileDialogTitle));
    public static string ImportWindow_NoQrCodeFoundError => Get(nameof(ImportWindow_NoQrCodeFoundError));
    public static string ImportWindow_UnrecognizedQrCodeError => Get(nameof(ImportWindow_UnrecognizedQrCodeError));
    public static string ImportWindow_MultiPartStatus => Get(nameof(ImportWindow_MultiPartStatus));
    public static string ImportWindow_NoAccountsFoundError => Get(nameof(ImportWindow_NoAccountsFoundError));
    public static string ImportWindow_DuplicateSkippedStatus => Get(nameof(ImportWindow_DuplicateSkippedStatus));
    public static string ImportWindow_UnknownIssuerPlaceholder => Get(nameof(ImportWindow_UnknownIssuerPlaceholder));

    #endregion

    #region ManageTagsWindow

    public static string ManageTagsWindow_Title => Get(nameof(ManageTagsWindow_Title));
    public static string ManageTagsWindow_MoveUpTooltip => Get(nameof(ManageTagsWindow_MoveUpTooltip));
    public static string ManageTagsWindow_MoveDownTooltip => Get(nameof(ManageTagsWindow_MoveDownTooltip));

    #endregion

    #region App

    public static string App_WindowsHelloPrompt => Get(nameof(App_WindowsHelloPrompt));
    public static string App_VerificationFailedMessage => Get(nameof(App_VerificationFailedMessage));

    #endregion
}