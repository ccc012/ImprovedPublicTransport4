using CSLModsCommon.Extension;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CSLModsCommon.UI.ValueFields;

public class TextDocument {
    public class UndoEntry {
        public string Text;
        public int Cursor;
        public int SelStart;
        public int SelEnd;
    }

    public event Action OnChanged;
    public event Action OnSelectionChanged;
    public event Action OnCursorChanged;

    private string _text = string.Empty;

    public string Text {
        get => _text;
        set {
            if (value == _text) return;
            _text = value ?? string.Empty;
            ClampCursorAndSelection();
            OnChanged?.Invoke();
        }
    }

    public int CursorIndex { get; private set; }
    public int SelectionStart { get; private set; } = 0;
    public int SelectionEnd { get; private set; } = 0;
    public int SelectionLength => SelectionEnd - SelectionStart;
    public string SelectedText => SelectionLength == 0 ? string.Empty : _text.Substring(SelectionStart, SelectionLength);

    private readonly List<UndoEntry> _undoStack = new();
    private readonly List<UndoEntry> _redoStack = new();
    public int UndoLimit { get; set; } = 20;
    private bool _isUndoing = false;

    public TextDocument() { }

    private void ClampCursorAndSelection() {
        CursorIndex = Mathf.Clamp(CursorIndex, 0, _text.Length);
        SelectionStart = Mathf.Clamp(SelectionStart, 0, _text.Length);
        SelectionEnd = Mathf.Clamp(SelectionEnd, 0, _text.Length);
    }

    private void PushUndoPoint() {
        if (_isUndoing) return;
        var entry = new UndoEntry { Text = _text, Cursor = CursorIndex, SelStart = SelectionStart, SelEnd = SelectionEnd };
        _undoStack.Add(entry);
        // trim
        if (UndoLimit > 0 && _undoStack.Count > UndoLimit) _undoStack.RemoveAt(0);
        // clear redo
        _redoStack.Clear();
    }

    public void SaveUndoPoint() => PushUndoPoint();

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public void Undo() {
        if (!CanUndo) return;
        _isUndoing = true;
        var last = _undoStack.Last();
        _undoStack.RemoveAt(_undoStack.Count - 1);

        // push current to redo
        _redoStack.Add(new UndoEntry { Text = _text, Cursor = CursorIndex, SelStart = SelectionStart, SelEnd = SelectionEnd });

        // restore
        _text = last.Text;
        CursorIndex = last.Cursor;
        SelectionStart = last.SelStart;
        SelectionEnd = last.SelEnd;

        ClampCursorAndSelection();
        _isUndoing = false;
        OnChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnCursorChanged?.Invoke();
    }

    public void Redo() {
        if (!CanRedo) return;
        _isUndoing = true;
        var last = _undoStack.Last();
        _redoStack.RemoveAt(_redoStack.Count - 1);

        // push current to undo
        _undoStack.Add(new UndoEntry { Text = _text, Cursor = CursorIndex, SelStart = SelectionStart, SelEnd = SelectionEnd });

        _text = last.Text;
        CursorIndex = last.Cursor;
        SelectionStart = last.SelStart;
        SelectionEnd = last.SelEnd;

        ClampCursorAndSelection();
        _isUndoing = false;
        OnChanged?.Invoke();
        OnSelectionChanged?.Invoke();
        OnCursorChanged?.Invoke();
    }

    // --- basic editing operations, they auto save undo points where appropriate ---

    private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

    public void Insert(string s) {
        if (s == null) return;
        PushUndoPoint();
        DeleteSelectionInternal();
        var sb = new StringBuilder(_text.Length + s.Length);
        sb.Append(_text);
        sb.Insert(CursorIndex, s);
        CursorIndex += s.Length;
        _text = sb.ToString();
        ClearSelection();
        OnChanged?.Invoke();
        OnCursorChanged?.Invoke();
    }

    public void InsertChar(char c) => Insert(c.ToString());

    private void DeleteSelectionInternal() {
        if (SelectionStart == SelectionEnd) return;
        _text = _text.Remove(SelectionStart, SelectionLength);
        CursorIndex = SelectionStart;
        ClearSelectionInternal();
    }

    public void DeleteSelection() {
        if (SelectionStart == SelectionEnd) return;
        PushUndoPoint();
        DeleteSelectionInternal();
        OnChanged?.Invoke();
        OnCursorChanged?.Invoke();
    }

    public void DeletePreviousChar() {
        if (SelectionStart != SelectionEnd) {
            DeleteSelection();
            return;
        }

        if (CursorIndex == 0) return;
        PushUndoPoint();
        _text = _text.Remove(CursorIndex - 1, 1);
        CursorIndex--;
        OnChanged?.Invoke();
        OnCursorChanged?.Invoke();
    }

    public void DeleteNextChar() {
        if (SelectionStart != SelectionEnd) {
            DeleteSelection();
            return;
        }

        if (CursorIndex >= _text.Length) return;
        PushUndoPoint();
        _text = _text.Remove(CursorIndex, 1);
        OnChanged?.Invoke();
        OnCursorChanged?.Invoke();
    }

    public int FindPreviousWord(int startIndex) {
        int i;
        for (i = startIndex; i > 0; i--) {
            var c = _text[i - 1];
            if (IsWordChar(c)) break;
        }

        for (var j = i; j >= 0; j--) {
            if (j == 0) {
                i = 0;
                break;
            }

            var c2 = _text[j - 1];
            if (!IsWordChar(c2)) {
                i = j;
                break;
            }
        }

        return i;
    }

    public int FindNextWord(int startIndex) {
        var length = _text.Length;
        var i = startIndex;
        for (var j = i; j < length; j++) {
            var c = _text[j];
            if (!IsWordChar(c)) {
                i = j;
                while (i < length && !IsWordChar(_text[i])) i++;
                return i;
            }

            if (j == length - 1) i = length;
        }

        while (i < length && !IsWordChar(_text[i])) i++;
        return i;
    }

    public void DeletePreviousWord() {
        if (CursorIndex == 0) return;
        PushUndoPoint();
        var pos = FindPreviousWord(CursorIndex);
        _text = _text.Remove(pos, CursorIndex - pos);
        CursorIndex = pos;
        OnChanged?.Invoke();
        OnCursorChanged?.Invoke();
    }

    public void DeleteNextWord() {
        if (CursorIndex >= _text.Length) return;
        PushUndoPoint();
        var pos = FindNextWord(CursorIndex);
        if (pos == CursorIndex) pos = _text.Length;
        _text = _text.Remove(CursorIndex, pos - CursorIndex);
        OnChanged?.Invoke();
        OnCursorChanged?.Invoke();
    }

    public void MoveCursor(int index, bool keepSelection = false) {
        index = Mathf.Clamp(index, 0, _text.Length);
        if (index == CursorIndex && !keepSelection)
            // nothing
            return;

        CursorIndex = index;
        if (!keepSelection) {
            ClearSelectionInternal();
        }
        else {
            // extend selection: if selection is empty, start at previous cursor
            if (SelectionStart == SelectionEnd) {
                SelectionStart = Mathf.Min(CursorIndex, index);
                SelectionEnd = Mathf.Max(CursorIndex, index);
            }
            else {
                // extend from anchor - we do not track anchor here; upper layer can manage anchor if needed
            }

            OnSelectionChanged?.Invoke();
        }

        OnCursorChanged?.Invoke();
    }

    public void SetSelection(int start, int end) {
        start = Mathf.Clamp(start, 0, _text.Length);
        end = Mathf.Clamp(end, 0, _text.Length);
        SelectionStart = Mathf.Min(start, end);
        SelectionEnd = Mathf.Max(start, end);
        OnSelectionChanged?.Invoke();
    }

    public void SetCursorAndSelection(int cursor, int selStart, int selEnd) {
        CursorIndex = Mathf.Clamp(cursor, 0, _text.Length);
        SelectionStart = Mathf.Clamp(selStart, 0, _text.Length);
        SelectionEnd = Mathf.Clamp(selEnd, 0, _text.Length);
        OnCursorChanged?.Invoke();
        OnSelectionChanged?.Invoke();
    }

    public void SelectAll() {
        SelectionStart = 0;
        SelectionEnd = _text.Length;
        CursorIndex = _text.Length;
        OnSelectionChanged?.Invoke();
        OnCursorChanged?.Invoke();
    }

    public void ClearSelection() {
        ClearSelectionInternal();
        OnSelectionChanged?.Invoke();
    }

    private void ClearSelectionInternal() => SelectionStart = SelectionEnd = CursorIndex;
}