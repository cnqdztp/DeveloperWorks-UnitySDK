mergeInto(LibraryManager.library, {

  DW_SetLocalStorage: function (key, value) {
    var keyStr = UTF8ToString(key);
    var valueStr = UTF8ToString(value);
    try {
      window.localStorage.setItem(keyStr, valueStr);
    } catch (e) {
      console.error("DW SDK: Failed to set localStorage:", e);
    }
  },

  DW_GetLocalStorage: function (key) {
    var keyStr = UTF8ToString(key);
    try {
      var value = window.localStorage.getItem(keyStr);
      if (value === null) {
        return null;
      }
      var bufferSize = lengthBytesUTF8(value) + 1;
      var buffer = _malloc(bufferSize);
      stringToUTF8(value, buffer, bufferSize);
      return buffer;
    } catch (e) {
      console.error("DW SDK: Failed to get localStorage:", e);
      return null;
    }
  },

  DW_RemoveLocalStorage: function (key) {
    var keyStr = UTF8ToString(key);
    try {
      window.localStorage.removeItem(keyStr);
    } catch (e) {
      console.error("DW SDK: Failed to remove localStorage:", e);
    }
  },

  DW_HasLocalStorageKey: function (key) {
    var keyStr = UTF8ToString(key);
    try {
      return window.localStorage.getItem(keyStr) !== null;
    } catch (e) {
      console.error("DW SDK: Failed to check localStorage key:", e);
      return false;
    }
  }

});