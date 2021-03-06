﻿using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using Zavolokas.ImageProcessing.PatchMatch;
using Zavolokas.Structures;

namespace InpaintService.Activities
{
    public static class NnfMergeActivity
    {
        public const string Name = "MergeNnfs";

        [FunctionName(Name)]
        public static Task MergeNnfs([ActivityTrigger] (string[] nnfs, string[] mappings, string nnf, string container, string mapping) input)
        {
            return Task.Run(() =>
            {
                var container = BlobHelper.OpenBlobContainer(input.container);
                var nnfState = BlobHelper.ReadFromBlob<NnfState>(input.nnfs[0], container);
                var destNnf = new Nnf(nnfState);

                var mappingState = BlobHelper.ReadFromBlob<Area2DMapState>(input.mappings[0], container);
                var destMapping = new Area2DMap(mappingState);

                var mapBuilder = new Area2DMapBuilder();

                for (int nnfIndex = 1; nnfIndex < input.nnfs.Length; nnfIndex++)
                {
                    nnfState = BlobHelper.ReadFromBlob<NnfState>(input.nnfs[nnfIndex], container);
                    var srcNnf = new Nnf(nnfState);

                    mappingState = BlobHelper.ReadFromBlob<Area2DMapState>(input.mappings[nnfIndex], container);
                    var srcMapping = new Area2DMap(mappingState);

                    destNnf.Merge(srcNnf, destMapping, srcMapping);

                    destMapping = mapBuilder
                        .InitNewMap(destMapping)
                        .AddMapping(srcMapping)
                        .Build();

                }

                var nnfData = JsonConvert.SerializeObject(destNnf.GetState());
                BlobHelper.SaveJsonToBlob(nnfData, container, input.nnf);
                foreach (var nnf in input.nnfs)
                {
                    BlobHelper.SaveJsonToBlob(nnfData, container, nnf);
                }
            });
        }
    }
}