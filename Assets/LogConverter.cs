using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Globalization;

/*
 * Takes in a log file from the Mirrorshades platform that has timestamps as absolute values
 * & head rotations as quaternions for every frame, outputs a new file that has elapsed time
 * in seconds & rotations in quaternions as well as individual x/y/z rotations for one frame
 * per second. This new format allows the log data to be plotted more easily as time doesn't
 * alter with framerate & rotations can be plotted meaningfully whilst raw quaternions can't.
 */
public class LogConverter : MonoBehaviour {

    // log file that will be converted, output filename is different (input file is not overwritten)
    public string filename;

    // stores the lines of the input file
    static string[] originalLog;

	// Use this for initialization
	void Start () {
        
        // read input file as lines into an array
        originalLog = File.ReadAllLines(filename);

        // stores the output lines
        List<string> newLog = new List<string>();

        // add header to the output with new headings for the rotations that are going to be added
        string header = "\"frame\"\t\"timestamp\"\t\"original_position\"\t\"position\"\t\"delta_x\"\t\"delta_z\"\t\"left_rotation\"\t\"left_x\"\t\"left_y\"\t\"left_z\"\t\"right_rotation\"\t\"right_x\"\t\"right_y\"\t\"right_z\"\t\"base_opacity\"\t\"left_opacity\"\t\"right_opacity\"\t\"auto_tick\"\t\"auto_duration\"\t\"auto_spacing\"\t\"framerate\"\t\"A_button\"\t\"B_button\"\t\"right_trigger\"";
        newLog.Add(header);

        // ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= =======
        // all subsequent lines of log data

        // get the absolute time at which the first line of log data occurred
        DateTime firstTime = DateTime.ParseExact(originalLog[1].Split('\t')[1], "dd-MM-yyyy HH-mm-ss-fff", CultureInfo.InvariantCulture);
        TimeSpan elapsed = new TimeSpan();

        // for each of the remaining lines of the log file
        string[] currentLine;
        string currentLineOut;
        int currentSecond = 0, currentMinute = 0;
        for (int i = 1; i < originalLog.Length; i++) {

            currentLine = originalLog[i].Split('\t');
            currentLineOut = currentLine[0] + "\t";

            // handle the first line of log data differently, as it will be 0 seconds elapsed
            if (i == 1) {
                currentLineOut += "0";
            }
            else {
                // calculate the elapsed time since the first line of log data & this current line of log data
                elapsed = (DateTime.ParseExact(originalLog[i].Split('\t')[1], "dd-MM-yyyy HH-mm-ss-fff", CultureInfo.InvariantCulture)) - firstTime;
                
                // truncate any fractions of seconds (milliseconds), add to output
                currentLineOut += elapsed.TotalSeconds.ToString().Split('.')[0];
            }

            // write fields 2 to 6 from the original log to the output
            for (int j = 2; j < 7; j++) {
                currentLineOut += "\t" + currentLine[j];
            }

            // calculate left orientation from quaternion as 3x rotations & output
            string[] leftQuaternion = currentLine[6].Split(',');

            leftQuaternion[0] = leftQuaternion[0].TrimStart('(');
            leftQuaternion[3] = leftQuaternion[3].TrimEnd(')');
            for (int k = 0; k < leftQuaternion.Length; k++) {
                leftQuaternion[k] = leftQuaternion[k].Trim();
            }

            Quaternion leftQuaternion_ = new Quaternion(Convert.ToSingle(leftQuaternion[0]), Convert.ToSingle(leftQuaternion[1]),
                                                        Convert.ToSingle(leftQuaternion[2]), Convert.ToSingle(leftQuaternion[3]));

            //currentLineOut += "\t" + leftQuaternion_.eulerAngles.x + "\t" + leftQuaternion_.eulerAngles.y + "\t"
            //               + leftQuaternion_.eulerAngles.z + "\t" + currentLine[7];

            // map orientations from -180 to 180 instead of 0 to 360
            currentLineOut += "\t" + (leftQuaternion_.eulerAngles.x > 180 ? (leftQuaternion_.eulerAngles.x - 360) : leftQuaternion_.eulerAngles.x)
                           + "\t" + (leftQuaternion_.eulerAngles.y > 180 ? (leftQuaternion_.eulerAngles.y - 360) : leftQuaternion_.eulerAngles.y)
                           + "\t" + (leftQuaternion_.eulerAngles.z > 180 ? (leftQuaternion_.eulerAngles.z - 360) : leftQuaternion_.eulerAngles.z)
                           + "\t" + currentLine[7];

            // calculate right orientation from quaternion as 3x rotations & output
            string[] rightQuaternion = currentLine[7].Split(',');
            rightQuaternion[0] = rightQuaternion[0].TrimStart('(');
            rightQuaternion[3] = rightQuaternion[3].TrimEnd(')');
            for (int k = 0; k < rightQuaternion.Length; k++) {
                rightQuaternion[k] = rightQuaternion[k].Trim();
            }

            Quaternion rightQuaternion_ = new Quaternion(Convert.ToSingle(rightQuaternion[0]), Convert.ToSingle(rightQuaternion[1]),
                                                        Convert.ToSingle(rightQuaternion[2]), Convert.ToSingle(rightQuaternion[3]));

            currentLineOut += "\t" + rightQuaternion_.eulerAngles.x + "\t" + rightQuaternion_.eulerAngles.y + "\t"
                           + rightQuaternion_.eulerAngles.z;

            // then do j = 8 to currentLine.Length
            for (int j = 8; j < currentLine.Length; j++) {
                currentLineOut += "\t" + currentLine[j];
            }

            // ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= =======

            // only output 1 line per second

            // special case to output 0 seconds line
            if (i == 1) {
                newLog.Add(currentLineOut);
            }
            else if (elapsed.Seconds > currentSecond) {
                currentSecond = elapsed.Seconds;
                currentMinute = elapsed.Minutes;
                newLog.Add(currentLineOut);
            }
            
            // catch the special case where seconds wraps back to 0 but is still 'greater than' 59 because of the minutes
            if ((elapsed.Seconds < currentSecond) && (elapsed.Minutes > currentMinute)) {
                currentSecond = elapsed.Seconds;
                currentMinute = elapsed.Minutes;
                newLog.Add(currentLineOut);
            }

        }

        // ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= ======= =======
        // after all lines of log data have been converted

        // write all of the edited lines to a new log file
        File.WriteAllLines((filename.Split('.')[0] + "_elapsed.log"), newLog.ToArray());

	}
	
	// Update is called once per frame
	void Update () {
	
	}
}