using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardMinus : MonoBehaviour
{
    private static BoardMinus instance;
    public static BoardMinus Instance { get { return instance; } }

    public Vector4 minus;

    public bool sedenje;
    public void ShraniMinus()
    {
        GameObject boardSetupObject = GameObject.Find("BoardManager");

        if (boardSetupObject != null)
        {
            Board boardSetupScript = boardSetupObject.GetComponent<Board>();

            if (boardSetupScript != null)
            {
                minus = boardSetupScript.minus;
                sedenje = boardSetupScript.sitting;
            }
            else
            {
                Debug.LogError("Board script not found on the GameObject.");
            }
        }
        else
        {
            Debug.LogError("GameObject with the Board script not found.");
        }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }
}
