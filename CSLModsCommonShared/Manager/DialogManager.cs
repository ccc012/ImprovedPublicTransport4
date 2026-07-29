using ColossalFramework;
using ColossalFramework.UI;
using CSLModsCommon.UI.Dialogs;
using UnityEngine;
using Animation = CSLModsCommon.Utilities.Animation;

namespace CSLModsCommon.Manager;

public class DialogManager : ManagerBase {
    private ModManagerBase _modManager;
    private SettingManager _settingManager;

    protected override void OnCreate() {
        base.OnCreate();
        _modManager = Domain.GetManager<ModManagerBase>();
        _settingManager = Domain.GetOrCreateManager<SettingManager>();
    }

    protected override void OnGameLoaded(LoadContext context) {
        base.OnGameLoaded(context);
        ShowLogDialog();
    }

    private void ShowLogDialog() {
        var defaultSetting = _settingManager.GetDefaultSetting();
        var lastVersion = defaultSetting.CurrentModVersion;
        var modVersion = ModVersion.FromVersion(_modManager.ModVersion);
        var versionType = _modManager.CurrentBuildChannel;

        if (versionType != BuildChannel.Alpha && versionType != BuildChannel.Beta) {
            if (modVersion.CompareWithoutRevision(lastVersion) > 0) {
                Show<ChangelogDialog>().Init();
            }
        }

        defaultSetting.CurrentModVersion = modVersion;
        _settingManager.SaveDefaultSetting();
    }

    public void Hide(DialogBase dialog) {
        if (!dialog) return;

        var top = UIView.GetModalComponent();
        if (top != dialog) return;
        UIView.PopModal();

        var modalEffect = UIView.GetAView().panelsLibraryModalEffect;
        if (modalEffect != null) {
            if (!UIView.HasModalInput())
                ValueAnimator.Animate("ModalEffect67419", val => modalEffect.opacity = val, new AnimatedFloat(1f, 0f, 0.7f, EasingType.CubicEaseOut), () => modalEffect.Hide());
            else
                modalEffect.zOrder = UIView.GetModalComponent().zOrder - 1;
        }

        if (dialog.UsingAnimation)
            Animation.AnimateOut(dialog, () => {
                if (UIView.GetModalComponent() == dialog)
                    UIView.PopModal();
                DestroyDialog(dialog);
            });
        else
            DestroyDialog(dialog);
    }

    public T Show<T>() where T : DialogBase {
        var gameObject = new GameObject {
            transform = {
                parent = UIView.GetAView().transform
            }
        };

        var dialog = gameObject.AddComponent<T>();
        dialog.Show(true);
        dialog.Focus();

        var modalEffect = UIView.GetAView().panelsLibraryModalEffect;
        if (modalEffect != null) {
            modalEffect.FitTo(null);
            if (!UIView.HasModalInput()) {
                modalEffect.Show(false);
                ValueAnimator.Animate("ModalEffect67419", val => modalEffect.opacity = val, new AnimatedFloat(0f, 1f, 0.7f, EasingType.CubicEaseOut));
            }

            modalEffect.zOrder = dialog.zOrder - 1;
        }

        UIView.PushModal(dialog);
        return dialog;
    }

    private void DestroyDialog(DialogBase dialog) {
        dialog.Hide();
        Object.Destroy(dialog.gameObject);
        Object.Destroy(dialog);
    }
}