using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BBAlphabet : MonoBehaviour
{
    [System.Serializable]
    public class LetterInstruction
    {
        public string letter;
        public BBLetter prefab;

        public bool Matches(string other, bool caseInsensitive = true)
        {
            return caseInsensitive ? letter.ToUpper() == other.ToUpper() : letter == other;
        }
    }

    [SerializeField]
    List<LetterInstruction> letters = new List<LetterInstruction>();

    Dictionary<string, List<BBLetter>> spawned = new Dictionary<string, List<BBLetter>>();

    public BBLetter Get(string letter)
    {
        BBLetter instance = null;
        if (spawned.TryGetValue(letter, out var instances))
        {
            instance = instances.FirstOrDefault(i => !i.gameObject.activeSelf);

            if (instance != null)
            {
                return instance;
            }
        }

        var instruction = letters.FirstOrDefault(l => l.Matches(letter));
        if (instruction == null)
        {
            Debug.LogWarning($"Requested letter: '{letter}' but we don't have any instructions for it");
            return null;
        }

        instance = Instantiate(instruction.prefab);
        
        if (!spawned.ContainsKey(letter))
        {
            spawned[letter] = new List<BBLetter>() { instance };
        } else
        {
            spawned[letter].Add(instance);
        }

        return instance;
    }
}
