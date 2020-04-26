﻿// See LICENSE.txt for license information.

namespace VictorBush.Ego.NefsLib.Header
{
    using System;
    using Microsoft.Extensions.Logging;
    using VictorBush.Ego.NefsLib.DataSource;
    using VictorBush.Ego.NefsLib.Item;

    /// <summary>
    /// A NeFS archive header.
    /// </summary>
    public class Nefs16Header : INefsHeader
    {
        /// <summary>
        /// Offset to the first data item used in most archives.
        /// </summary>
        public const UInt32 DataOffsetDefault = 0x10000U;

        /// <summary>
        /// Offset to the first data item used in large archives where header needs more room.
        /// </summary>
        public const UInt32 DataOffsetLarge = 0x50000U;

        /// <summary>
        /// Offset to the header intro.
        /// </summary>
        public const uint IntroOffset = 0x0;

        private static readonly ILogger Log = NefsLog.GetLogger();

        /// <summary>
        /// Initializes a new instance of the <see cref="Nefs16Header"/> class.
        /// </summary>
        /// <param name="intro">Header intro.</param>
        /// <param name="toc">Header intro table of contents.</param>
        /// <param name="part1">Header part 1.</param>
        /// <param name="part2">Header part 2.</param>
        /// <param name="part3">Header part 3.</param>
        /// <param name="part4">Header part 4.</param>
        /// <param name="part5">Header part 5.</param>
        /// <param name="part6">Header part 6.</param>
        /// <param name="part7">Header part 7.</param>
        /// <param name="part8">Header part 8.</param>
        public Nefs16Header(
            NefsHeaderIntro intro,
            Nefs16HeaderIntroToc toc,
            Nefs16HeaderPart1 part1,
            Nefs16HeaderPart2 part2,
            Nefs16HeaderPart3 part3,
            Nefs16HeaderPart4 part4,
            Nefs16HeaderPart5 part5,
            Nefs16HeaderPart6 part6,
            Nefs16HeaderPart7 part7,
            Nefs16HeaderPart8 part8)
        {
            this.Intro = intro ?? throw new ArgumentNullException(nameof(intro));
            this.TableOfContents = toc ?? throw new ArgumentNullException(nameof(toc));
            this.Part1 = part1 ?? throw new ArgumentNullException(nameof(part1));
            this.Part2 = part2 ?? throw new ArgumentNullException(nameof(part2));
            this.Part3 = part3 ?? throw new ArgumentNullException(nameof(part3));
            this.Part4 = part4 ?? throw new ArgumentNullException(nameof(part4));
            this.Part5 = part5 ?? throw new ArgumentNullException(nameof(part5));
            this.Part6 = part6 ?? throw new ArgumentNullException(nameof(part6));
            this.Part7 = part7 ?? throw new ArgumentNullException(nameof(part7));
            this.Part8 = part8 ?? throw new ArgumentNullException(nameof(part8));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Nefs16Header"/> class.
        /// </summary>
        /// <param name="intro">Header intro.</param>
        /// <param name="toc">Header intro table of contents.</param>
        /// <param name="items">List of items.</param>
        public Nefs16Header(NefsHeaderIntro intro, Nefs16HeaderIntroToc toc, NefsItemList items)
        {
            this.Intro = intro ?? throw new ArgumentNullException(nameof(intro));
            this.TableOfContents = toc ?? throw new ArgumentNullException(nameof(toc));

            this.Part3 = new Nefs16HeaderPart3(items);
            this.Part4 = new Nefs16HeaderPart4(items);

            this.Part1 = new Nefs16HeaderPart1(items, this.Part4);
            this.Part2 = new Nefs16HeaderPart2(items, this.Part3);
            this.Part5 = new Nefs16HeaderPart5();
            this.Part6 = new Nefs16HeaderPart6(items);
            this.Part7 = new Nefs16HeaderPart7(items);
            this.Part8 = new Nefs16HeaderPart8(intro.HeaderSize - toc.OffsetToPart8);
        }

        /// <summary>
        /// The header intro.
        /// </summary>
        public NefsHeaderIntro Intro { get; }

        /// <inheritdoc/>
        public Boolean IsEncrypted => this.Intro.IsEncrypted;

        /// <summary>
        /// Header part 1.
        /// </summary>
        public Nefs16HeaderPart1 Part1 { get; }

        /// <summary>
        /// Header part 2.
        /// </summary>
        public Nefs16HeaderPart2 Part2 { get; }

        /// <summary>
        /// Header part 3.
        /// </summary>
        public Nefs16HeaderPart3 Part3 { get; }

        /// <summary>
        /// Header part 4.
        /// </summary>
        public Nefs16HeaderPart4 Part4 { get; }

        /// <summary>
        /// Header part 5.
        /// </summary>
        public Nefs16HeaderPart5 Part5 { get; }

        /// <summary>
        /// Header part 6.
        /// </summary>
        public Nefs16HeaderPart6 Part6 { get; }

        /// <summary>
        /// Header part 7.
        /// </summary>
        public Nefs16HeaderPart7 Part7 { get; }

        /// <summary>
        /// Header part 8.
        /// </summary>
        public Nefs16HeaderPart8 Part8 { get; }

        /// <summary>
        /// The header intro table of contents.
        /// </summary>
        public Nefs16HeaderIntroToc TableOfContents { get; }

        /// <inheritdoc/>
        public NefsItem CreateItemInfo(NefsItemId id, NefsItemList dataSourceList)
        {
            var p1 = this.Part1.EntriesById[id];
            var p2 = this.Part2.EntriesById[id];

            // Check if part 6 exists
            Nefs16HeaderPart6Entry p6 = null;
            if (this.Part6.EntriesById.ContainsKey(id))
            {
                p6 = this.Part6.EntriesById[id];
            }

            // Determine type
            var type = p2.Data0x0c_ExtractedSize.Value == 0 ? NefsItemType.Directory : NefsItemType.File;

            // Find parent
            var parentId = this.GetItemDirectoryId(id);

            // Offset and size
            var dataOffset = p1.Data0x00_OffsetToData.Value;
            var extractedSize = p2.Data0x0c_ExtractedSize.Value;

            // Transform
            var transform = new NefsDataTransform(NefsHeaderIntroToc.ChunkSize, true, this.Intro.IsEncrypted ? this.Intro.GetAesKey() : null);

            // Data source
            INefsDataSource dataSource;
            if (type == NefsItemType.Directory)
            {
                // Item is a directory
                dataSource = new NefsEmptyDataSource();
                transform = null;
            }
            else if (p1.IndexIntoPart4 == 0xFFFFFFFFU)
            {
                // Item is not compressed
                var size = new NefsItemSize(extractedSize);
                dataSource = new NefsItemListDataSource(dataSourceList, dataOffset, size);
            }
            else
            {
                // Item is compressed
                var chunks = this.Part4.CreateChunksListForItem(id, this.Intro.GetAesKey());
                var size = new NefsItemSize(extractedSize, chunks);
                dataSource = new NefsItemListDataSource(dataSourceList, dataOffset, size);
            }

            // File name and path
            var fileName = this.GetItemFileName(id);

            // Gather unknown metadata
            var unknown = new NefsItemUnknownData
            {
                Part6Unknown0x00 = p6?.Byte0 ?? 0,
                Part6Unknown0x01 = p6?.Byte1 ?? 0,
                Part6Unknown0x02 = p6?.Byte2 ?? 0,
                Part6Unknown0x03 = p6?.Byte3 ?? 0,
            };

            // Create item
            return new NefsItem(id, fileName, parentId, type, dataSource, transform, unknown);
        }

        /// <inheritdoc/>
        public NefsItemList CreateItemList(String dataFilePath)
        {
            var items = new NefsItemList(dataFilePath);

            foreach (var entry in this.Part1.EntriesById)
            {
                var id = entry.Key;

                try
                {
                    var item = this.CreateItemInfo(id, items);
                    items.Add(item);
                }
                catch (Exception)
                {
                    Log.LogError($"Failed to create item {id}, skipping.");
                }
            }

            return items;
        }

        /// <inheritdoc/>
        public NefsItemId GetItemDirectoryId(NefsItemId id)
        {
            return new NefsItemId(this.Part2.EntriesById[id].Data0x00_DirectoryId.Value);
        }

        /// <inheritdoc/>
        public string GetItemFileName(NefsItemId id)
        {
            var offsetIntoPart3 = this.Part2.EntriesById[id].Data0x08_OffsetIntoPart3.Value;
            return this.Part3.FileNamesByOffset[offsetIntoPart3];
        }
    }
}
