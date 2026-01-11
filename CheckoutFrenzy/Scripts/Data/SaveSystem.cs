using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace CryingSnow.CheckoutFrenzy
{
    public static class SaveSystem
    {
        /// <summary>
        /// Saves data to a file with a specified name.
        /// </summary>
        /// <typeparam name="T">The type of the data to be saved.</typeparam>
        /// <param name="data">The data to be saved.</param>
        /// <param name="fileName">The name of the file to save the data to.</param>
        public static void SaveData<T>(T data, string fileName)
        {
            // Construct the full path to the save file
            string filePath = Application.persistentDataPath + "/" + fileName + ".dat";

            // Create a new BinaryFormatter for serialization
            BinaryFormatter formatter = new BinaryFormatter();

            // Open a file stream to create or overwrite the file
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                // Serialize the data to the file
                formatter.Serialize(fileStream, data);
            }
        }

        /// <summary>
        /// Loads data from a file with a specified name.
        /// </summary>
        /// <typeparam name="T">The type of the data to be loaded.</typeparam>
        /// <param name="fileName">The name of the file to load the data from.</param>
        /// <returns>The loaded data, or default(T) if the file does not exist.</returns>
        public static T LoadData<T>(string fileName)
        {
            // Construct the full path to the save file
            string filePath = Application.persistentDataPath + "/" + fileName + ".dat";

            // Check if the file exists before attempting to load it
            if (File.Exists(filePath))
            {
                // Create a new BinaryFormatter for deserialization
                BinaryFormatter formatter = new BinaryFormatter();

                // Open a file stream to read the file
                using (FileStream fileStream = new FileStream(filePath, FileMode.Open))
                {
                    // Deserialize the data from the file
                    T data = (T)formatter.Deserialize(fileStream);

                    return data;
                }
            }
            else
            {
                // Optional: Log an error message if the file does not exist
                // Debug.LogError("Save file not found in " + filePath);

                // Return default value if the file does not exist
                return default(T);
            }
        }
    }
}
