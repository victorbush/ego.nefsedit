﻿// See LICENSE.txt for license information.

namespace VictorBush.Ego.NefsEdit.Commands
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Command buffer that supports undo/redo.
    /// </summary>
    internal class UndoBuffer
    {
        private readonly List<INefsEditCommand> commands = new List<INefsEditCommand>();

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoBuffer"/> class.
        /// </summary>
        public UndoBuffer()
        {
            this.Reset();
        }

        /// <summary>
        /// Raised when a command is executed (new, undo, or redo).
        /// </summary>
        public event EventHandler<NefsEditCommandEventArgs> CommandExecuted;

        /// <summary>
        /// Gets a value indicating whether a redo is available.
        /// </summary>
        public bool CanRedo => this.NextCommandIndex < this.commands.Count && this.NextCommandIndex >= 0;

        /// <summary>
        /// Gets a value indicating whether an undo is available.
        /// </summary>
        public bool CanUndo => this.PreviousCommandIndex >= 0 && this.PreviousCommandIndex < this.commands.Count;

        /// <summary>
        /// Gets the current commands list.
        /// </summary>
        public IReadOnlyList<INefsEditCommand> Commands => this.commands;

        /// <summary>
        /// Gets a value indicating whether the undo buffer is in a modified state.
        /// </summary>
        public bool IsModified => this.PreviousCommandIndex != this.SavedCommandIndex;

        /// <summary>
        /// Gets the index of the next command (for redo).
        /// </summary>
        public int NextCommandIndex { get; private set; }

        /// <summary>
        /// Gets the index of the previous command (for undo).
        /// </summary>
        public int PreviousCommandIndex { get; private set; }

        /// <summary>
        /// Gets the index of the command that represents the unmodified state for a file.
        /// </summary>
        public int SavedCommandIndex { get; private set; }

        /// <summary>
        /// Executes a command and adds it to the undo buffer.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        public void Execute(INefsEditCommand command)
        {
            command.Do();

            // Clear any redo commands (can only undo after executing a new command)
            if (this.NextCommandIndex < this.commands.Count)
            {
                this.commands.RemoveRange(this.NextCommandIndex, this.commands.Count - this.NextCommandIndex);
            }

            // Add command
            this.commands.Add(command);

            // Update command index
            this.PreviousCommandIndex = this.NextCommandIndex;
            this.NextCommandIndex = this.commands.Count;

            // Notifiy command executed
            var eventArgs = new NefsEditCommandEventArgs(NefsEditCommandEventKind.New, command);
            this.CommandExecuted?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Marks the current undo buffer state as the unmodified state.
        /// </summary>
        public void MarkAsSaved()
        {
            this.SavedCommandIndex = this.PreviousCommandIndex;
        }

        /// <summary>
        /// Executes the next command in the undo buffer, if available.
        /// </summary>
        /// <returns>True if the redo was executed.</returns>
        public bool Redo()
        {
            if (!this.CanRedo)
            {
                return false;
            }

            var command = this.commands[this.NextCommandIndex];
            command.Do();
            this.NextCommandIndex += 1;
            this.PreviousCommandIndex += 1;

            var eventArgs = new NefsEditCommandEventArgs(NefsEditCommandEventKind.Redo, command);
            this.CommandExecuted?.Invoke(this, eventArgs);
            return true;
        }

        /// <summary>
        /// Resets the undo buffer.
        /// </summary>
        public void Reset()
        {
            this.commands.Clear();
            this.NextCommandIndex = 0;
            this.PreviousCommandIndex = -1;
            this.SavedCommandIndex = -1;
        }

        /// <summary>
        /// Reverts the previous command in the undo buffer, if available.
        /// </summary>
        /// <returns>True if the undo was executed.</returns>
        public bool Undo()
        {
            if (!this.CanUndo)
            {
                return false;
            }

            var command = this.commands[this.PreviousCommandIndex];
            command.Undo();
            this.PreviousCommandIndex -= 1;
            this.NextCommandIndex -= 1;

            var eventArgs = new NefsEditCommandEventArgs(NefsEditCommandEventKind.Undo, command);
            this.CommandExecuted?.Invoke(this, eventArgs);
            return true;
        }
    }
}
