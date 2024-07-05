using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Barebones.Networking;

namespace Barebones.MasterServer
{
    public enum SaveLoadOpCodes
    {
        GetSaves = 20,
        GetLevels = 21,
        GetNumberOfLevels = 22,
        GetLevelProps = 23,
        StreamTimeHistory = 24,
        GetRaidLevel = 25,
        GetCompetitiveLevel = 26,
        GetChallengeLevel = 27,
        GetNumberOfCompLevels = 28,
        GetNumberOfChalLevels = 29,
        GetLoadingHint = 30

    }
    public class SaveLoadModule : ServerModuleBehaviour
    {
        //For checking if a user is loggged in before selecting a level

        public void Awake()
        {
            AddDependency<AuthModule>();
        }


        public override void Initialize(IServer server)
        {
            base.Initialize(server);


            server.SetHandler((short)SaveLoadOpCodes.GetSaves, HandleGetSaves);
            server.SetHandler((short)SaveLoadOpCodes.GetLevels, HandleGetLevel);
            server.SetHandler((short)SaveLoadOpCodes.GetNumberOfLevels, HandleNumberOfLevels);
            server.SetHandler((short)SaveLoadOpCodes.GetLevelProps, HandleGetLevelProps);
            server.SetHandler((short)SaveLoadOpCodes.StreamTimeHistory, HandleStreamTimeHistory);
            server.SetHandler((short)SaveLoadOpCodes.GetRaidLevel, HandleGetRaidLevel);
            server.SetHandler((short)SaveLoadOpCodes.GetCompetitiveLevel, HandleGetCompLevel);
            server.SetHandler((short)SaveLoadOpCodes.GetNumberOfCompLevels, HandleGetNumberOfCompLevels);
            server.SetHandler((short)SaveLoadOpCodes.GetNumberOfChalLevels, HandleGetNumberOfChalLevels);
            server.SetHandler((short)SaveLoadOpCodes.GetChallengeLevel, HandleGetChallLevel);
            server.SetHandler((short)SaveLoadOpCodes.GetLoadingHint, HandleGetLoadHint);
        }

        #region Handlers
        private void HandleGetSaves(IIncommingMessage message)
        {


        }
        private void HandleGetLoadHint(IIncommingMessage message)
        {
            var db = Msf.Server.DbAccessors.GetAccessor<ISaveDatabase>();
            db.GetLoadingHint((hint) =>
            {
                if(hint==null)
                {
                    message.Respond(ResponseStatus.Failed);
                }
                else
                {
                    message.Respond(hint, ResponseStatus.Success);
                }
                
            });

        }
        private void HandleGetLevel(IIncommingMessage message)
        {
            
                var db = Msf.Server.DbAccessors.GetAccessor<ISaveDatabase>();

                int levelID = message.AsInt();

                db.GetLevel(levelID, LevelData =>
                {
                    if (LevelData == null)
                        message.Respond(ResponseStatus.Invalid);
                    else
                    {
                        var response = new LoadLevelPacket()
                        {
                            levelID = LevelData.LevelID,
                            grainNumber = LevelData.GrainNumber,
                            fileName = LevelData.FileName,
                            levelName = LevelData.LevelName

                        };

                        message.Respond(response, ResponseStatus.Success);
                    }
                });
            
            

        }

        private void HandleNumberOfLevels(IIncommingMessage message)
        {
            var db = Msf.Server.DbAccessors.GetAccessor<ISaveDatabase>();

            db.GetNumberOfLevels(numLevels =>
            {
                if (numLevels != -1)
                {
                    message.Respond(numLevels.ToString(), ResponseStatus.Success);
                }
                else
                    message.Respond(-1, ResponseStatus.Invalid);
            });
        }

        private void HandleGetLevelProps(IIncommingMessage message)
        {
            var db = Msf.Server.DbAccessors.GetAccessor<ISaveDatabase>();

            db.GetLevelProperties(message.AsInt(), (levelProps) =>
             {

                 if (levelProps == null)
                     message.Respond(ResponseStatus.Invalid);
                 else
                 {
                     LevelPropsPacket packet = new LevelPropsPacket
                     {
                         CompletionScore = levelProps.CompletionScore,
                         TimeLimit = levelProps.TimeLimit,
                         MoveLimit = levelProps.MoveLimit,
                         TargetScore = levelProps.TargetScore,
                         Condition = levelProps.Completion,
                         Mechanics = levelProps.Mechanics,
                         FrozenGrains = levelProps.FrozenGrains
                     };

                     message.Respond(packet, ResponseStatus.Success);
                 }

             });
        }

        private void HandleStreamTimeHistory(IIncommingMessage message)
        {
            var bytes = message.AsBytes();
            
            using(var stream = new MemoryStream(bytes))
            {
                using(var reader = new EndianBinaryReader(EndianBitConverter.Big, stream))
                {
                    string fileName = reader.ReadString();
                    string thFilePathName = fileName;
#if UNITY_STANDALONE_LINUX

                    string thLocation = Application.persistentDataPath + @"/THData/";

                    thFilePathName = thLocation + fileName;
#endif
                    using (FileStream writeFile = new FileStream(thFilePathName, FileMode.Create))
                    {
                        stream.WriteTo(writeFile);
                    }
                }
                
            }

            message.Respond(ResponseStatus.Success);
            Logs.Info("Successfully wrote stream to file");
        }

        private void HandleGetRaidLevel(IIncommingMessage message)
        {
            int levelID = message.AsInt();
            var db = Msf.Server.DbAccessors.GetAccessor<ISaveDatabase>();

            db.GetRaidLevel(levelID, (raidData) =>
            {
                if (raidData == null)
                {
                    Logs.Error("Could not get raid level from database");
                    message.Respond(ResponseStatus.Failed);
                }
                else
                {
                    RaidDataPacket packet = new RaidDataPacket
                    {
                        levelData = new LoadLevelPacket
                        {
                            levelID = raidData.LevelID,
                            levelName = raidData.LevelName,
                            fileName = raidData.FileName,
                            grainNumber = raidData.GrainNumber
                        },
                        levelProps = new LevelPropsPacket
                        {
                            CompletionScore = raidData.CompletionScore,
                            TimeLimit = raidData.TimeLimit,
                            MoveLimit = raidData.MoveLimit,
                            TargetScore = raidData.TargetScore,
                            Condition = raidData.Completion,
                            Mechanics = raidData.Mechanics,
                            FrozenGrains = raidData.FrozenGrains
                        },
                        description = raidData.Description
                    };

                    message.Respond(packet, ResponseStatus.Success);

                }
            });

        }

        private void HandleGetNumberOfCompLevels(IIncommingMessage message)
        {
            var db = Msf.Server.DbAccessors.GetAccessor<ISaveDatabase>();

            db.GetNumberOfCompLevels(numLevels =>
            {
                if(numLevels < 0)
                {
                    message.Respond(numLevels, ResponseStatus.Failed);

                }
                message.Respond(numLevels, ResponseStatus.Success);
            });
        }
        private void HandleGetCompLevel(IIncommingMessage message)
        {
            int levelID = message.AsInt();
            var db = Msf.Server.DbAccessors.GetAccessor<ISaveDatabase>();

            db.GetCompetitiveLevel(levelID, (compData) =>
            {
                if (compData == null)
                {
                    Logs.Error("Could not get competitive level from database");
                    message.Respond(ResponseStatus.Failed);
                }
                else
                {
                    CompetitiveDataPacket packet = new CompetitiveDataPacket
                    {
                        levelData = new LoadLevelPacket
                        {
                            levelID = compData.LevelID,
                            levelName = compData.LevelName,
                            fileName = compData.FileName,
                            grainNumber = compData.GrainNumber
                        },
                        levelProps = new LevelPropsPacket
                        {
                            CompletionScore = compData.CompletionScore,
                            TimeLimit = compData.TimeLimit,
                            MoveLimit = compData.MoveLimit,
                            TargetScore = compData.TargetScore,
                            Condition = compData.Completion,
                            Mechanics = compData.Mechanics,
                            FrozenGrains = compData.FrozenGrains
                        },
                        description = compData.Description
                    };

                    message.Respond(packet, ResponseStatus.Success);

                }
            });
        }

        private void HandleGetNumberOfChalLevels(IIncommingMessage message)
        {
            var db = Msf.Server.DbAccessors.GetAccessor<ISaveDatabase>();
            
            db.GetNumberOfChallLevels(numLevels =>
            {
                if (numLevels < 0)
                {
                    message.Respond(numLevels, ResponseStatus.Failed);

                }
                message.Respond(numLevels, ResponseStatus.Success);
            });
        }
        
        private void HandleGetChallLevel(IIncommingMessage message)
        {
            int levelID = message.AsInt();
            var db = Msf.Server.DbAccessors.GetAccessor<ISaveDatabase>();

            db.GetChallengeLevel(levelID, (compData) =>
            {
                if (compData == null)
                {
                    Logs.Error("Could not get competitive level from database");
                    message.Respond(ResponseStatus.Failed);
                }
                else
                {
                    CompetitiveDataPacket packet = new CompetitiveDataPacket
                    {
                        levelData = new LoadLevelPacket
                        {
                            levelID = compData.LevelID,
                            levelName = compData.LevelName,
                            fileName = compData.FileName,
                            grainNumber = compData.GrainNumber
                        },
                        levelProps = new LevelPropsPacket
                        {
                            CompletionScore = compData.CompletionScore,
                            TimeLimit = compData.TimeLimit,
                            MoveLimit = compData.MoveLimit,
                            TargetScore = compData.TargetScore,
                            Condition = compData.Completion,
                            Mechanics = compData.Mechanics,
                            FrozenGrains = compData.FrozenGrains
                        },
                        description = compData.Description
                    };

                    message.Respond(packet, ResponseStatus.Success);

                }
            });
        }
        #endregion
    }
}

