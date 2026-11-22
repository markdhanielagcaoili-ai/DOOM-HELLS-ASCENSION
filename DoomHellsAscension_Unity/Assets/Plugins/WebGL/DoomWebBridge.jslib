mergeInto(LibraryManager.library, {
  DoomNotifyReturnToMainMenu: function () {
    if (typeof window !== "undefined" && typeof window.doomReturnToMainMenu === "function") {
      window.doomReturnToMainMenu();
      return 1;
    }
    return 0;
  },
  DoomNotifyRestoreMainMenuDirect: function () {
    if (typeof window !== "undefined" && typeof window.doomRestoreMainMenuDirect === "function") {
      window.doomRestoreMainMenuDirect();
      return 1;
    }
    return 0;
  }
});
