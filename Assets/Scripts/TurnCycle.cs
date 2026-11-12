using System.Collections.Generic;
using System.Linq;

// Ordre de jeu pur et testable : joueur courant, passage au suivant, numéro de manche.
public class TurnCycle<T> where T : class
{
    private readonly List<T> order;
    public IReadOnlyList<T> Order => order;
    public int Index { get; private set; }
    public int Round { get; private set; } = 1;
    public T Current => order.Count > 0 ? order[Index] : null;

    // `first` joue en premier (s'il est non nul), puis les autres dans l'ordre de la liste ; les nuls sont ignorés.
    public TurnCycle(IEnumerable<T> players, T first)
    {
        order = new List<T>();
        if (first != null) order.Add(first);
        order.AddRange(players.Where(p => p != null && p != first));
    }

    // Passe au joueur suivant. Renvoie true si une nouvelle manche commence.
    public bool Advance()
    {
        if (order.Count == 0) return false;
        Index++;
        if (Index < order.Count) return false;
        Index = 0;
        Round++;
        return true;
    }
}
