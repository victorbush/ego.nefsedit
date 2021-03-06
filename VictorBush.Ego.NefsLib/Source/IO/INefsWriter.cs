﻿// See LICENSE.txt for license information.

namespace VictorBush.Ego.NefsLib.IO
{
    using System.Threading.Tasks;
    using VictorBush.Ego.NefsLib.Progress;

    /// <summary>
    /// Writes NeFS archives.
    /// </summary>
    public interface INefsWriter
    {
        /// <summary>
        /// Writes the archive to the specified desintation.
        /// </summary>
        /// <param name="destFilePath">The path to write the file to.</param>
        /// <param name="nefs">The archive to write.</param>
        /// <param name="p">Progress info.</param>
        /// <returns>An updated archive object containing updated metadata.</returns>
        Task<NefsArchive> WriteArchiveAsync(string destFilePath, NefsArchive nefs, NefsProgress p);
    }
}
