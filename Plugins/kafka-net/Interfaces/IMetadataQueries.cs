﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityMultimediaStreaming.Plugins.Protocol;

namespace UnityMultimediaStreaming.Plugins.Interfaces
{
    /// <summary>
    /// Contains common metadata query commands that are used by both a consumer and producer.
    /// </summary>
    interface IMetadataQueries : IDisposable
    {
        /// <summary>
        /// Get metadata on the given topic.
        /// </summary>
        /// <param name="topic">The metadata on the requested topic.</param>
        /// <returns>Topic object containing the metadata on the requested topic.</returns>
        Topic GetTopic(string topic);

        /// <summary>
        /// Get offsets for each partition from a given topic.
        /// </summary>
        /// <param name="topic">Name of the topic to get offset information from.</param>
        /// <param name="maxOffsets"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        Task<List<OffsetResponse>> GetTopicOffsetAsync(string topic, int maxOffsets = 2, int time = -1);
    }
}
