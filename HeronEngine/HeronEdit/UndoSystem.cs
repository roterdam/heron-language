using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HeronEdit
{
    public class UndoSystem
    {
        Stack<Action> undos = new Stack<Action>();
        Stack<Action> redos = new Stack<Action>();
        bool suspend = false;

        public void AddUndo(Action undo)
        {
            redos.Clear();
            if (!suspend)
                undos.Push(undo);
        }

        public void AddUndo(string name, Action.Procedure action, Action.Procedure undo)
        {
            AddUndo(new Action(name, action, undo, null));
        }

        public void AddUndo(string name, Action.Procedure action, Action.Procedure undo, Object tag)
        {
            AddUndo(new Action(name, action, undo, tag));
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
            Action redo = undos.Pop();
            redo.Undo();
            redos.Push(redo);
        }

        public void Redo()
        {
            if (!CanRedo()) return;
            Action undo = redos.Pop();
            undo.Undo();
            undos.Push(undo);
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

    public class Action
    {
        public delegate void Procedure();

        Procedure undo;
        Procedure action;

        public Object Tag { get; set; }
        public String Name { get; set; }

        public Action(String name, Procedure action, Procedure undo, Object tag)
        {
            this.action = action;
            this.undo = undo;
            Tag = tag;
            Name = name;
        }

        public Action Undo()
        {
            undo();
            Procedure tmp = undo;
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
