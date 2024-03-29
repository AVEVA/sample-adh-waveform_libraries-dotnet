﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OSIsoft.Data;
using OSIsoft.Data.Http;
using OSIsoft.Data.Reflection;
using OSIsoft.Identity;

namespace SdsClientLibraries
{
    public static class Program
    {
        private static IConfiguration _configuration;
        private static Exception _toThrow;

        public static void Main() => MainAsync().GetAwaiter().GetResult();

        public static async Task<bool> MainAsync(bool test = false)
        {            
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.test.json", optional: true);
            _configuration = builder.Build();

            // ==== Client constants ====
            string tenantId = _configuration["TenantId"];
            string namespaceId = _configuration["NamespaceId"];
            string resource = _configuration["Resource"];
            string clientId = _configuration["ClientId"];
            string clientSecret = _configuration["ClientSecret"];

            // ==== Metadata IDs ====
            string streamId = "SampleStream_Dotnet";
            string streamIdSecondary = "SampleStream_Secondary_Dotnet";
            string streamIdCompound = "SampleStream_Compound_Dotnet";

            string typeId = "SampleType_Dotnet";
            string targetTypeId = "SampleType_Target_Dotnet";
            string targetIntTypeId = "SampleType_TargetInt_Dotnet";
            string autoStreamViewId = "SampleAutoStreamView_Dotnet";
            string manualStreamViewId = "SampleManualStreamView_Dotnet";
            string compoundTypeId = "SampleType_Compound_Dotnet";

            Uri uriResource = new (resource);
            
            // Step 1 
            // Get Sds Services to communicate with server
            AuthenticationHandler authenticationHandler = new (uriResource, clientId, clientSecret);
            
            SdsService sdsService = new (new Uri(resource), null, HttpCompressionMethod.GZip, authenticationHandler);
            ISdsMetadataService metadataService = sdsService.GetMetadataService(tenantId, namespaceId);
            ISdsDataService dataService = sdsService.GetDataService(tenantId, namespaceId);
            ISdsTableService tableService = sdsService.GetTableService(tenantId, namespaceId);

            Console.WriteLine(@"-------------------------------------------------------------");
            Console.WriteLine(@"  _________    .___           _______  ______________________");
            Console.WriteLine(@" /   _____/  __| _/______     \      \ \_   _____/\__    ___/");
            Console.WriteLine(@" \_____  \  / __ |/  ___/     /   |   \ |    __)_   |    |   ");
            Console.WriteLine(@" /        \/ /_/ |\___ \     /    |    \|        \  |    |   ");
            Console.WriteLine(@"/_______  /\____ /____  > /\ \____|__  /_______  /  |____|   ");
            Console.WriteLine(@"        \/      \/    \/  \/         \/        \/            ");
            Console.WriteLine(@"-------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine($"SDS endpoint at {resource}");
            Console.WriteLine();

            try
            {
                // Step 2
                // create an SdsType
                Console.WriteLine("Creating an SdsType");
                SdsType type = SdsTypeBuilder.CreateSdsType<WaveData>();
                type.Id = typeId;
                type = await metadataService.GetOrCreateTypeAsync(type).ConfigureAwait(false);

                // Step 3
                // create an SdsStream
                Console.WriteLine("Creating an SdsStream");
                SdsStream stream = new ()
                {
                    Id = streamId,
                    Name = "Wave Data Sample",
                    TypeId = type.Id,
                    Description = "This is a sample SdsStream for storing WaveData type measurements",
                };
                stream = await metadataService.GetOrCreateStreamAsync(stream).ConfigureAwait(false);

                // Step 4
                // insert data
                Console.WriteLine("Inserting data");

                // insert a single event
                WaveData wave = GetWave(0, 2);
                await dataService.InsertValueAsync(stream.Id, wave).ConfigureAwait(false);

                // insert a list of events
                List<WaveData> waves = new ();
                for (int i = 2; i <= 18; i += 2)
                {
                    waves.Add(GetWave(i, 2));
                }

                await dataService.InsertValuesAsync(stream.Id, waves).ConfigureAwait(false);

                // Step 5
                // get last event
                Console.WriteLine("Getting latest event");
                WaveData latest = await dataService.GetLastValueAsync<WaveData>(streamId).ConfigureAwait(false);
                Console.WriteLine(latest.ToString());
                Console.WriteLine();

                // get all events
                Console.WriteLine("Getting all events");
                List<WaveData> allEvents = (List<WaveData>)await dataService.GetWindowValuesAsync<WaveData>(streamId, "0", "180").ConfigureAwait(false);
                Console.WriteLine($"Total events found: {allEvents.Count}");
                foreach (WaveData evnt in allEvents)
                {
                    Console.WriteLine(evnt.ToString());
                }

                Console.WriteLine();

                // Step 6
                // getting all events in table format with headers
                SdsTable tableEvents = await tableService.GetWindowValuesAsync(stream.Id, "0", "180").ConfigureAwait(false);

                Console.WriteLine("Getting table events");
                foreach (IList<object> evnt in tableEvents.Rows)
                {
                    Console.WriteLine(string.Join(",", evnt.ToArray()));
                }

                Console.WriteLine();

                // Step 7
                // update events
                Console.WriteLine("Updating events");

                // update one event
                WaveData updatedWave = UpdateWave(allEvents.First(), 4);
                await dataService.UpdateValueAsync(stream.Id, updatedWave).ConfigureAwait(false);

                // update all events, adding ten more
                List<WaveData> updatedCollection = new ();
                for (int i = 2; i < 40; i += 2)
                {
                    updatedCollection.Add(GetWave(i, 4));
                }

                await dataService.UpdateValuesAsync(stream.Id, updatedCollection).ConfigureAwait(false);

                allEvents = (List<WaveData>)await dataService.GetWindowValuesAsync<WaveData>(stream.Id, "0", "180").ConfigureAwait(false);

                Console.WriteLine("Getting updated events");
                Console.WriteLine($"Total events found: {allEvents.Count}");

                foreach (WaveData evnt in allEvents)
                {
                    Console.WriteLine(evnt.ToString());
                }

                Console.WriteLine();

                // Step 8
                // replacing events
                Console.WriteLine("Replacing events");

                // replace one event
                WaveData replaceEvent = UpdateWave(allEvents.First(), multiplier: 5);

                await dataService.ReplaceValueAsync<WaveData>(streamId, replaceEvent).ConfigureAwait(false);

                // replace all events
                for (int i = 1; i < allEvents.Count; i++)
                {
                    allEvents[i] = UpdateWave(allEvents[i], multiplier: 5);
                }

                await dataService.ReplaceValuesAsync<WaveData>(streamId, allEvents).ConfigureAwait(false);

                Console.WriteLine("Getting replaced events");
                List<WaveData> replacedEvents = (List<WaveData>)await dataService.GetWindowValuesAsync<WaveData>(streamId, "0", "180").ConfigureAwait(false);
                Console.WriteLine($"Total events found: {replacedEvents.Count}");
                foreach (WaveData evnt in replacedEvents)
                {
                    Console.WriteLine(evnt.ToString());
                }

                Console.WriteLine();

                // Step 9
                // Property Overrides
                Console.WriteLine("SDS can interpolate or extrapolate data at an index location where data does not explicitly exist:");
                Console.WriteLine();

                // We will retrieve three events using the default behavior, Continuous
                IEnumerable<WaveData> retrieved = await dataService
                    .GetRangeValuesAsync<WaveData>(stream.Id, "1", 3, SdsBoundaryType.ExactOrCalculated).ConfigureAwait(false);
                Console.WriteLine("Default (Continuous) requesting data starting at index location '1', where we have not entered data, SDS will interpolate a value for this property and then return entered values:");
                foreach (WaveData value in retrieved)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine();

                IEnumerable<WaveData> retrievedInterpolated = await dataService
                    .GetValuesAsync<WaveData>(stream.Id, "5", "32", 4).ConfigureAwait(false);
                Console.WriteLine("SDS will interpolate a value for each index asked for (5,14,23,32):");
                foreach (WaveData value in retrievedInterpolated)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine();

                // Step 10
                // We will retrieve events filtered to only get the ones where the radians are less than 50.  Note, this can be done on index properties too.
                IEnumerable<WaveData> retrievedInterpolatedFiltered = await dataService.GetWindowFilteredValuesAsync<WaveData>(stream.Id, "0", "180", SdsBoundaryType.ExactOrCalculated, "Radians lt 50").ConfigureAwait(false);
                Console.WriteLine("SDS will only return the values where the radians are less than 50:");
                foreach (WaveData value in retrievedInterpolatedFiltered)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine();

                // Step 11
                // We will retrieve a sample of our data
                Console.WriteLine("SDS can return a sample of your data population to show trends.");
                Console.WriteLine("Getting Sampled Values:");
                IEnumerable<WaveData> sampledValues = await dataService.GetSampledValuesAsync<WaveData>(stream.Id, "0",
                    "40", 4, new[] { nameof(WaveData.Sin) }).ConfigureAwait(false);
                foreach (WaveData sample in sampledValues)
                {
                    Console.WriteLine(sample);
                }

                Console.WriteLine();

                // Step 12
                // create a Discrete stream PropertyOverride indicating that we do not want Sds to calculate a value for Radians and update our stream 
                SdsStreamPropertyOverride propertyOverride = new ()
                {
                    SdsTypePropertyId = "Radians",
                    InterpolationMode = SdsInterpolationMode.Discrete,
                };
                List<SdsStreamPropertyOverride> propertyOverrides = new () { propertyOverride };

                // update the stream
                stream.PropertyOverrides = propertyOverrides;
                await metadataService.CreateOrUpdateStreamAsync(stream).ConfigureAwait(false);

                retrieved = await dataService
                    .GetRangeValuesAsync<WaveData>(stream.Id, "1", 3, SdsBoundaryType.ExactOrCalculated).ConfigureAwait(false);
                Console.WriteLine("We can override this behavior on a property by property basis, here we override the Radians property instructing Sds not to interpolate.");
                Console.WriteLine("SDS will now return the default value for the data type:");

                foreach (WaveData value in retrieved)
                {
                    Console.WriteLine(value.ToString());
                }

                Console.WriteLine();

                // Step 13
                // StreamViews
                Console.WriteLine("SdsStreamViews");

                // create target types
                SdsType targetType = SdsTypeBuilder.CreateSdsType<WaveDataTarget>();
                targetType.Id = targetTypeId;

                SdsType targetIntType = SdsTypeBuilder.CreateSdsType<WaveDataInteger>();
                targetIntType.Id = targetIntTypeId;

                await metadataService.GetOrCreateTypeAsync(targetType).ConfigureAwait(false);
                await metadataService.GetOrCreateTypeAsync(targetIntType).ConfigureAwait(false);

                // create StreamViews
                SdsStreamView autoStreamView = new ()
                {
                    Id = autoStreamViewId,
                    SourceTypeId = typeId,
                    TargetTypeId = targetTypeId,
                };

                // create explicit mappings 
                SdsStreamViewProperty vp1 = new () { SourceId = "Order", TargetId = "OrderTarget" };
                SdsStreamViewProperty vp2 = new () { SourceId = "Sin", TargetId = "SinInt" };
                SdsStreamViewProperty vp3 = new () { SourceId = "Cos", TargetId = "CosInt" };
                SdsStreamViewProperty vp4 = new () { SourceId = "Tan", TargetId = "TanInt" };

                SdsStreamView manualStreamView = new ()
                {
                    Id = manualStreamViewId,
                    SourceTypeId = typeId,
                    TargetTypeId = targetIntTypeId,
                    Properties = new List<SdsStreamViewProperty>() { vp1, vp2, vp3, vp4 },
                };

                await metadataService.CreateOrUpdateStreamViewAsync(autoStreamView).ConfigureAwait(false);
                await metadataService.CreateOrUpdateStreamViewAsync(manualStreamView).ConfigureAwait(false);

                Console.WriteLine("Here is some of our data as it is stored on the server:");
                foreach (WaveData evnt in retrieved)
                {
                    Console.WriteLine($"Sin: {evnt.Sin}, Cos: {evnt.Cos}, Tan {evnt.Tan}");
                }

                Console.WriteLine();

                // get autoStreamView data
                IEnumerable<WaveDataTarget> autoStreamViewData = await dataService.GetRangeValuesAsync<WaveDataTarget>(stream.Id, "1", 3, SdsBoundaryType.ExactOrCalculated, autoStreamViewId).ConfigureAwait(false);

                Console.WriteLine("Specifying a StreamView with an SdsType of the same shape returns values that are automatically mapped to the target SdsType's properties:");

                foreach (WaveDataTarget value in autoStreamViewData)
                {
                    Console.WriteLine($"SinTarget: {value.SinTarget} CosTarget: {value.CosTarget} TanTarget: {value.TanTarget}");
                }

                Console.WriteLine();

                // get manualStreamView data
                Console.WriteLine("SdsStreamViews can also convert certain types of data, here we return integers where the original values were doubles:");
                IEnumerable<WaveDataInteger> manualStreamViewData = await dataService.GetRangeValuesAsync<WaveDataInteger>(stream.Id, "1", 3, SdsBoundaryType.ExactOrCalculated, manualStreamViewId).ConfigureAwait(false);

                foreach (WaveDataInteger value in manualStreamViewData)
                {
                    Console.WriteLine($"SinInt: {value.SinInt} CosInt: {value.CosInt} TanInt: {value.TanInt}");
                }

                Console.WriteLine();

                // get SdsStreamViewMap
                Console.WriteLine("We can query SDS to return the SdsStreamViewMap for our SdsStreamView, here is the one generated automatically:");
                SdsStreamViewMap autoStreamViewMap = await metadataService.GetStreamViewMapAsync(autoStreamViewId).ConfigureAwait(false);
                PrintStreamViewMapProperties(autoStreamViewMap);

                Console.WriteLine("Here is our explicit mapping, note SdsStreamViewMap will return all properties of the Source Type, even those without a corresponding Target property:");
                SdsStreamViewMap manualStreamViewMap = await metadataService.GetStreamViewMapAsync(manualStreamViewId).ConfigureAwait(false);
                PrintStreamViewMapProperties(manualStreamViewMap);

                // Step 14
                // Update Stream Type based on SdsStreamView
                Console.WriteLine("We will now update the stream type based on the stream view");

                WaveData firstVal = await dataService.GetFirstValueAsync<WaveData>(stream.Id).ConfigureAwait(false);

                await metadataService.UpdateStreamTypeAsync(stream.Id, autoStreamViewId).ConfigureAwait(false);
                SdsStream newStream = await metadataService.GetStreamAsync(stream.Id).ConfigureAwait(false);

                WaveDataTarget firstValUpdated = await dataService.GetFirstValueAsync<WaveDataTarget>(stream.Id).ConfigureAwait(false);

                Console.WriteLine($"The new type id {newStream.TypeId} compared to the original one {stream.TypeId}.");
                Console.WriteLine($"The new type value {firstValUpdated} compared to the original one {firstVal}.");
                Console.WriteLine();

                // Step 15
                // Show filtering on Type, works the same as filtering on Streams
                IEnumerable<SdsType> types = await metadataService.GetTypesAsync().ConfigureAwait(false);
                IEnumerable<SdsType> typesFiltered = await metadataService.GetTypesAsync("Id:*Target*").ConfigureAwait(false);

                Console.WriteLine($"The number of types returned without filtering: {types.Count()}.  With filtering {typesFiltered.Count()}.");
                Console.WriteLine();

                // Step 16
                // tags and metadata
                Console.WriteLine("Let's add some Tags and Metadata to our stream:");
                List<string> tags = new () { "waves", "periodic", "2018", "validated" };
                Dictionary<string, string> metadata = new () { { "Region", "North America" }, { "Country", "Canada" }, { "Province", "Quebec" } };

                await metadataService.UpdateStreamTagsAsync(streamId, tags).ConfigureAwait(false);
                await metadataService.UpdateStreamMetadataAsync(streamId, metadata).ConfigureAwait(false);

                tags = (List<string>)await metadataService.GetStreamTagsAsync(streamId).ConfigureAwait(false);

                Console.WriteLine();
                Console.WriteLine($"Tags now associated with {streamId}:");
                foreach (string tag in tags)
                {
                    Console.WriteLine(tag);
                }

                Console.WriteLine();
                Console.WriteLine($"Metadata now associated with {streamId}:");
                Console.WriteLine("Metadata key Region: " + await metadataService.GetStreamMetadataValueAsync(streamId, "Region").ConfigureAwait(false));
                Console.WriteLine("Metadata key Country: " + await metadataService.GetStreamMetadataValueAsync(streamId, "Country").ConfigureAwait(false));
                Console.WriteLine("Metadata key Province: " + await metadataService.GetStreamMetadataValueAsync(streamId, "Province").ConfigureAwait(false));
                Console.WriteLine();

                // Step 17
                // update metadata
                Console.WriteLine("Let's make some changes to the Metadata on our stream:");
                MetadataPatchDocument patch = new ();
                patch.Remove("Region");
                patch.Replace("Province", "Ontario");
                patch.Add("City", "Toronto");
                await metadataService.PatchStreamMetadataAsync(streamId, patch).ConfigureAwait(false);

                Console.WriteLine();
                Console.WriteLine($"Metadata now associated with {streamId}:");
                Console.WriteLine("Metadata key Country: " + await metadataService.GetStreamMetadataValueAsync(streamId, "Country").ConfigureAwait(false));
                Console.WriteLine("Metadata key Province: " + await metadataService.GetStreamMetadataValueAsync(streamId, "Province").ConfigureAwait(false));
                Console.WriteLine("Metadata key City: " + await metadataService.GetStreamMetadataValueAsync(streamId, "City").ConfigureAwait(false));
                Console.WriteLine();

                // Step 18
                // delete values
                Console.WriteLine("Deleting values from the SdsStream");

                // delete one event
                await dataService.RemoveValueAsync(stream.Id, 0).ConfigureAwait(false);

                // delete all events
                await dataService.RemoveWindowValuesAsync(stream.Id, 1, 200).ConfigureAwait(false);

                retrieved = await dataService.GetWindowValuesAsync<WaveData>(stream.Id, "0", "200").ConfigureAwait(false);
                if (retrieved.ToList().Count == 0)
                {
                    Console.WriteLine("All values deleted successfully!");
                }

                Console.WriteLine();

                // Step 19
                // Adding a new stream with a secondary index.
                Console.WriteLine("Adding a stream with a secondary index.");

                SdsStreamIndex measurementIndex = new ()
                {
                    SdsTypePropertyId = type.Properties.First(p => p.Id.Equals("Radians", StringComparison.OrdinalIgnoreCase)).Id,
                };

                SdsStream secondary = new ()
                {
                    Id = streamIdSecondary,
                    TypeId = type.Id,
                    Indexes = new List<SdsStreamIndex>()
                    {
                        measurementIndex,
                    },
                };

                secondary = await metadataService.GetOrCreateStreamAsync(secondary).ConfigureAwait(false);
                Console.WriteLine($"Secondary indexes on streams. {stream.Id}:{stream.Indexes?.Count}. {secondary.Id}:{secondary.Indexes.Count}. ");
                Console.WriteLine();

                // Modifying an existing stream with a secondary index.
                Console.WriteLine("Modifying a stream to have a secondary index.");

                stream = await metadataService.GetStreamAsync(stream.Id).ConfigureAwait(false);
                type = await metadataService.GetTypeAsync(stream.TypeId).ConfigureAwait(false);

                SdsStreamIndex measurementTargetIndex = new ()
                {
                    SdsTypePropertyId = type.Properties.First(p => p.Id.Equals("RadiansTarget", StringComparison.OrdinalIgnoreCase)).Id,
                };
                stream.Indexes = new List<SdsStreamIndex>() { measurementTargetIndex };

                await metadataService.CreateOrUpdateStreamAsync(stream).ConfigureAwait(false);
                stream = await metadataService.GetStreamAsync(stream.Id).ConfigureAwait(false);

                // Modifying an existing stream to remove the secondary index
                Console.WriteLine("Removing a secondary index from a stream.");

                secondary.Indexes = null;
                await metadataService.CreateOrUpdateStreamAsync(secondary).ConfigureAwait(false);
                secondary = await metadataService.GetStreamAsync(secondary.Id).ConfigureAwait(false);
                Console.WriteLine($"Secondary indexes on streams. {stream.Id}:{stream.Indexes?.Count}. {secondary.Id}:{secondary.Indexes?.Count}. ");
                Console.WriteLine();

                // Step 20
                // Adding Compound Index Type
                Console.WriteLine("Creating an SdsType with a compound index");
                SdsType typeCompound = SdsTypeBuilder.CreateSdsType<WaveDataCompound>();
                typeCompound.Id = compoundTypeId;
                typeCompound = await metadataService.GetOrCreateTypeAsync(typeCompound).ConfigureAwait(false);

                // create an SdsStream
                Console.WriteLine("Creating an SdsStream off of type with compound index");
                SdsStream streamCompound = new ()
                {
                    Id = streamIdCompound,
                    Name = "Wave Data Sample",
                    TypeId = typeCompound.Id,
                    Description = "This is a sample SdsStream for storing WaveData type measurements",
                };
                streamCompound = await metadataService.GetOrCreateStreamAsync(streamCompound).ConfigureAwait(false);

                // Step 21
                // insert compound data
                Console.WriteLine("Inserting data");
                await dataService.InsertValueAsync(streamCompound.Id, GetWaveMultiplier(1, 10)).ConfigureAwait(false);
                await dataService.InsertValueAsync(streamCompound.Id, GetWaveMultiplier(2, 2)).ConfigureAwait(false);
                await dataService.InsertValueAsync(streamCompound.Id, GetWaveMultiplier(3, 1)).ConfigureAwait(false);
                await dataService.InsertValueAsync(streamCompound.Id, GetWaveMultiplier(10, 3)).ConfigureAwait(false);
                await dataService.InsertValueAsync(streamCompound.Id, GetWaveMultiplier(10, 8)).ConfigureAwait(false);
                await dataService.InsertValueAsync(streamCompound.Id, GetWaveMultiplier(10, 10)).ConfigureAwait(false);

                WaveDataCompound latestCompound = await dataService.GetLastValueAsync<WaveDataCompound>(streamCompound.Id).ConfigureAwait(false);
                WaveDataCompound firstCompound = await dataService.GetFirstValueAsync<WaveDataCompound>(streamCompound.Id).ConfigureAwait(false);

                IEnumerable<WaveDataCompound> data = await dataService.GetWindowValuesAsync<WaveDataCompound, int, int>(streamCompound.Id, Tuple.Create(2, 1), Tuple.Create(10, 8)).ConfigureAwait(false);

                Console.WriteLine($"First data: {firstCompound}.  Latest data: {latestCompound}.");

                Console.WriteLine();

                Console.WriteLine("Window Data:");

                foreach (WaveDataCompound evnt in data)
                {
                    Console.WriteLine(evnt.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                _toThrow = ex;
            }
            finally
            {
                // Step 22
                Console.WriteLine();
                Console.WriteLine("Cleaning up");

                // Delete the stream, types and streamViews making sure
                Console.WriteLine("Deleting stream");
                await RunInTryCatch(metadataService.DeleteStreamAsync, streamId).ConfigureAwait(false);
                await RunInTryCatch(metadataService.DeleteStreamAsync, streamIdSecondary).ConfigureAwait(false);
                await RunInTryCatch(metadataService.DeleteStreamAsync, streamIdCompound).ConfigureAwait(false);
                Console.WriteLine("Deleting stream views");
                await RunInTryCatch(metadataService.DeleteStreamViewAsync, autoStreamViewId).ConfigureAwait(false);
                await RunInTryCatch(metadataService.DeleteStreamViewAsync, manualStreamViewId).ConfigureAwait(false);
                Console.WriteLine("Deleting types");
                await RunInTryCatch(metadataService.DeleteTypeAsync, typeId).ConfigureAwait(false);
                await RunInTryCatch(metadataService.DeleteTypeAsync, compoundTypeId).ConfigureAwait(false);
                await RunInTryCatch(metadataService.DeleteTypeAsync, targetTypeId).ConfigureAwait(false);
                await RunInTryCatch(metadataService.DeleteTypeAsync, targetIntTypeId).ConfigureAwait(false);

                Console.WriteLine("Complete!");
                if (!test)
                    Console.ReadKey();
            }

            if (test && _toThrow != null)
                throw _toThrow;
            return _toThrow == null;
        }

        /// <summary>
        /// Use this to run a method that you don't want to stop the program if there is an error
        /// </summary>
        /// <param name="methodToRun">The method to run.</param>
        /// <param name="value">The value to put into the method to run</param>
        private static async Task RunInTryCatch(Func<string, Task> methodToRun, string value)
        {
            try
            {
                await methodToRun(value).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Got error in {methodToRun.Method.Name} with value {value} but continued on:" + ex.Message);
                if (_toThrow == null)
                {
                    _toThrow = ex;
                }
            }
        }

        private static void PrintStreamViewMapProperties(SdsStreamViewMap sdsStreamViewMap)
        {
            foreach (SdsStreamViewMapProperty prop in sdsStreamViewMap.Properties)
            {
                if (prop.TargetId != null)
                {
                    Console.WriteLine($"{prop.SourceId} => {prop.TargetId}");
                }
                else
                {
                    Console.WriteLine($"{prop.SourceId} => Not Mapped");
                }
            }

            Console.WriteLine();
        }

        private static WaveData GetWave(int order, double multiplier)
        {
            double radians = order * (Math.PI / 32);

            return new WaveData
            {
                Order = order,
                Radians = radians,
                Tau = radians / (2 * Math.PI),
                Sin = multiplier * Math.Sin(radians),
                Cos = multiplier * Math.Cos(radians),
                Tan = multiplier * Math.Tan(radians),
                Sinh = multiplier * Math.Sinh(radians),
                Cosh = multiplier * Math.Cosh(radians),
                Tanh = multiplier * Math.Tanh(radians),
            };
        }

        private static WaveData UpdateWave(WaveData wave, double multiplier)
        {
            wave.Tau = wave.Radians / (2 * Math.PI);
            wave.Sin = multiplier * Math.Sin(wave.Radians);
            wave.Cos = multiplier * Math.Cos(wave.Radians);
            wave.Tan = multiplier * Math.Tan(wave.Radians);
            wave.Sinh = multiplier * Math.Sinh(wave.Radians);
            wave.Cosh = multiplier * Math.Cosh(wave.Radians);
            wave.Tanh = multiplier * Math.Tanh(wave.Radians);

            return wave;
        }

        private static WaveDataCompound GetWaveMultiplier(int order, int multiplier)
        {
            double radians = order * (Math.PI / 32);

            return new WaveDataCompound
            {
                Order = order,
                Radians = radians,
                Tau = radians / (2 * Math.PI),
                Sin = multiplier * Math.Sin(radians),
                Cos = multiplier * Math.Cos(radians),
                Tan = multiplier * Math.Tan(radians),
                Sinh = multiplier * Math.Sinh(radians),
                Cosh = multiplier * Math.Cosh(radians),
                Tanh = multiplier * Math.Tanh(radians),
                Multiplier = multiplier,
            };
        }
    }
}
