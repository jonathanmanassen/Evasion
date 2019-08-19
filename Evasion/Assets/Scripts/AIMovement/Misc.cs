using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Misc : MonoBehaviour
{
    /// <summary>
    /// Returns a color according to the value given
    /// </summary>
    public static Color GetColorWithVar(int color)
    {
        if (color == 0)
        {
            return (Color.black);
        }
        else if (color == 1)
        {
            return (Color.blue);
        }
        else if (color == 2)
        {
            return (Color.cyan);
        }
        else if (color == 3)
        {
            return (Color.green);
        }
        else if (color == 4)
        {
            return (Color.magenta);
        }
        else if (color == 5)
        {
            return (Color.yellow);
        }
        else if (color == 6)
        {
            return (Color.black);
        }
        else if (color == 7)
        {
            return (Color.grey);
        }
        else
        {
            return (Color.white);
        }
    }
}
