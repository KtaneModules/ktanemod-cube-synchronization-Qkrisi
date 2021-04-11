using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;

public partial class CubeSynchronizationModule
{
    #pragma warning disable 414
    [HideInInspector] public string TwitchHelpMessage = "Use '!{0} submit <number>' to submit the final number!";
    #pragma warning restore 414
    
    IEnumerator ProcessTwitchCommand(string command)
    {
        var match = Regex.Match(command, @"^submit ([0-9]*)$", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            yield return null;
            yield return String.Format("sendtochaterror Invalid syntax! Please use '!{0} submit <positive number>'!", TwitchID);
            yield break;
        }
        string number = match.Groups[1].Value;
        int counter = 0;
        foreach (char digit in number)
        {
            counter++;
            if (!ReadyToSolve)
            {
                yield return null;
                yield return "sendtochaterror Module is not ready to be solved yet!";
                yield break;
            }
            foreach (Transform btn in Buttons)
            {
                NumberedButton BtnObject = btn.GetComponent<NumberedButton>();
                if (digit == BtnObject.Digit.ToString()[0])
                {
                    yield return null;
                    BtnObject.Click(() =>
                        SendTwitchMessageFormat("Cube Synchronization (#{0}) struck on character #{1}; digit: {2} (in input).", TwitchID,
                            counter, digit));
                    yield return new WaitForSeconds(.1f);
                }
            }
        }
    }
}