using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Util : MonoBehaviour
{
    public static string GetAbbreviation(string phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            return string.Empty;
        }

        string abbreviation = "";

        foreach (char c in phrase)
        {
            if (char.IsUpper(c)) // Include all uppercase letters
            {
                abbreviation += c;
            }
        }

        return abbreviation;
    }
}
