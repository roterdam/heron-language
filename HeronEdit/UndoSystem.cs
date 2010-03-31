using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEdit
{
    public class UndoSystem
    {
        Stack<UndoableAction> undos = new Stack<UndoableAction>();
        Stack<UndoableAction> redos = new Stack<UndoableAction>();
        bool suspend = false;

        public void AddUndo(UndoableAction undo)
        {
            if (suspend)
                return;
            redos.Clear();
            undos.Push(undo);
        }

        public void AddUndo(string name, Action action, Action undo)
        {
            AddUndo(new UndoableAction(name, action, undo, null));
        }

        public void AddUndo(string name, Action action, Action undo, Object tag)
        {
            AddUndo(new UndoableAction(name, action, undo, tag));
        }

        public bool CanUndo()
        {
            return undos.Count > 0;
        }

        public bool CanRedo()
        {
            return redos.Count > 0;
        }

        public void Undo()
        {
            if (!CanUndo()) return;
            try
            {
                suspend = true;
                UndoableAction redo = undos.Pop();
                redo.Undo();
                redos.Push(redo);
            }
            finally
            {
                suspend = false;
            }
        }

        public void Redo()
        {
            if (!CanRedo()) return;
            try
            {
                suspend = true;
                UndoableAction undo = redos.Pop();
                undo.Undo();
                undos.Push(undo);
            }
            finally
            {
                suspend = false;
            }
        }

        public Object LastActionTag()
        {
            if (!CanUndo())
                return null;
            return undos.Peek().Tag;
        }

        public event EventHandler Undone;
        public event EventHandler Redone;
        
        protected void OnUndone()
        {
            if (Undone != null)
                Undone(this, new EventArgs());
        }

        protected void OnRedone()
        {
            if (Redone != null)
                Redone(this, new EventArgs());
        }

        public void Clear()
        {
            undos.Clear();
            redos.Clear();
        }

        public void Suspend()
        {
            suspend = true;
        }

        public void Unsuspend()
        {
            suspend = false;
        }
    }

    public class UndoableAction
    {
        Action undo;
        Action action;

        public Object Tag { get; set; }
        public String Name { get; set; }

        public UndoableAction(String name, Action action, Action undo, Object tag)
        {
            this.action = action;
            this.undo = undo;
            Tag = tag;
            Name = name;
        }

        public UndoableAction Undo()
        {
            undo();
            Action tmp = undo;
            undo = action;
            action = tmp;
            return this;
        }

        public void Apply()
        {
            action();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
