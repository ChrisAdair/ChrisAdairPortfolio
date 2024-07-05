using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Data;
using Barebones.MasterServer;
using System.Text;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;

public class SaveFunctions : NetworkBehaviour{

    [Header("Set in Inspector")]
    public GrainNetworkAssigner controller;
    public string[] saveFiles;
    public bool pushToDB;

    private static readonly int key = 129;

    public void SaveGame()
    {
        if (controller == null)
        {
            controller = GameObject.Find("GrainNetwork(Clone)").GetComponent<GrainNetworkAssigner>();
        }

        if (!isServer)
        {
            Debug.LogError("Save function should only be called on server");
            return;
        }
        else
        {
            SaveStructure();
        }
    }

    [Server]
    private void SaveStructure()
    {
        //NOTE: Find functions only find active game objects
        GameObject[] vertices = GameObject.FindGameObjectsWithTag("Vertex");
        GameObject[] orientations = GameObject.FindGameObjectsWithTag("Orientation");
        TextAsset triangles = controller.TriList;
        TextAsset grainVerts = controller.GrainVerts;
        TextAsset grainNeighbors = controller.NeighborList;
        
        string saveName = orientations.Length.ToString() + "Date"  + ".SAVE";
#if UNITY_STANDALONE_WIN
        string saveLocation = Application.persistentDataPath + @"\Save Files\";
#else
        string saveLocation = Application.persistentDataPath + @"/Save Files/";
#endif
        bool saved = false;

        //If the save file directory does not exist, create one
        if(!Directory.Exists(saveLocation))
        {
            Directory.CreateDirectory(saveLocation);
        }
        using (StreamWriter writer = File.CreateText(saveLocation + saveName))
        {
            writer.WriteLine("! Vertices");
            foreach(GameObject vert in vertices)
            {
                Vector3 pos = vert.transform.position;
                writer.WriteLine(pos.x.ToString() + " " + pos.y.ToString() + " " + pos.z.ToString());
            }

            writer.WriteLine("! Orientations");
            foreach(GameObject orient in orientations)
            {
                Quaternion rot = orient.transform.rotation;
                writer.WriteLine(rot.w + " " + rot.x + " " + rot.y + " " + rot.z);
            }
            writer.WriteLine("! Triangles");
            writer.Write(triangles.text);
            writer.WriteLine();
            writer.WriteLine("! Grain Vertices");
            writer.Write(grainVerts.text);
            writer.WriteLine();
            writer.WriteLine("! Grain Neighbors");
            writer.Write(grainNeighbors.text);
            saved = true;
        }
        if (saved && pushToDB)
        {
            string connectionCode = Msf.Args.DbConnectionString;
            using (var mySqlConn = new MySqlConnection(connectionCode))
            using (var saveCmd = new MySqlCommand())
            {
                try
                {
                    mySqlConn.Open();
                    saveCmd.Connection = mySqlConn;
                    saveCmd.CommandText = "INSERT INTO save_data (file_name, user_owner, time_played, root_save)  VALUES (@filename, @userowner, NULL, NULL)";

                    saveCmd.Parameters.AddWithValue("@filename", saveName);
                    Msf.Server.Auth.GetPeerAccountInfo(0, (info, error) => {

                        saveCmd.Parameters.AddWithValue("@userowner", info.Username);
                    });
                    

                    saveCmd.ExecuteNonQuery();
                }
                catch(MySqlException e)
                {
                    Debug.LogWarning(e.ToString());
                }
            }
        }
        
    }

    [Server]
    public string[] GetSaves()
    {

        string saveLocation = Application.persistentDataPath + @"/Save Files/";
        //If the save file directory does not exist, create one
        if (!Directory.Exists(saveLocation))
        {
            Directory.CreateDirectory(saveLocation);
        }
        return Directory.GetFiles(saveLocation, "*.SAVE");
    }

    
    public static TextAsset[] LoadFile(string filePath)
    {
        TextAsset[] file = new TextAsset[5];

        LoadStructure(filePath, out file[0], out file[1], out file[2], out file[3], out file[4]);

        return file;
    }

    private static void LoadStructure(string filePath, out TextAsset vertices, out TextAsset orientations, out TextAsset triangles, out TextAsset grainVerts, out TextAsset grainNeighbors)
    {
        if(!File.Exists(filePath))
        {
            vertices = orientations = triangles = grainVerts = grainNeighbors = null;
            Debug.LogError("Structure file not found!");
            return;
        }


        using (StreamReader encoded = new StreamReader(filePath))
        {
            string encodedFile = encoded.ReadToEnd();
            //Decrypt the level file
            StringBuilder inString = new StringBuilder(encodedFile);
            StringBuilder outString = new StringBuilder(encodedFile.Length);
            StringBuilder checkString = new StringBuilder(encodedFile.Length);
            char decoded;
            char recoded;
            for (int i = 0; i < encodedFile.Length; i++)
            {
                decoded = inString[i];
                decoded = (char)((uint)decoded ^ (uint)key);
                recoded = (char)((uint)decoded ^ (uint)key);
                outString.Append(decoded);
                checkString.Append(recoded);
            }
            //using (StreamWriter tempWrite = new StreamWriter(Application.persistentDataPath + @"\Save Files\10GrainLevelEncrypt.SAVE"))
            //{
            //    tempWrite.Write(outString.ToString());
            //}

            //Read the decrypted file
            using (StringReader reader = new StringReader(outString.ToString()))
            {
                string verts = "";
                string temp = "";
                temp = reader.ReadLine();

                while ((temp = reader.ReadLine()) != "! Orientations")
                {
                    verts += temp + '\r' + '\n';
                }
                verts = verts.TrimEnd('\r', '\n', ' ');
                vertices = new TextAsset(verts);


                string orient = "";
                while ((temp = reader.ReadLine()) != "! Triangles")
                {
                    orient += temp + '\r' + '\n';
                }
                orient = orient.TrimEnd('\r', '\n', ' ');
                orientations = new TextAsset(orient);

                string tris = "";
                while ((temp = reader.ReadLine()) != "! Grain Vertices")
                {
                    tris += temp + '\r' + '\n';
                }
                tris = tris.TrimEnd('\r', '\n', ' ');
                triangles = new TextAsset(tris);

                string gVerts = "";
                while ((temp = reader.ReadLine()) != "! Grain Neighbors")
                {
                    gVerts += temp + '\r' + '\n';
                }
                gVerts = gVerts.TrimEnd('\r', '\n', ' ');
                grainVerts = new TextAsset(gVerts);

                string gNeighbors = reader.ReadToEnd();
                grainNeighbors = new TextAsset(gNeighbors);
            }



        }



    }
}
