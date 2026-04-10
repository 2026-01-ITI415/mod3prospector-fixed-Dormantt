using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Golf Solitaire - main game manager.
/// Attach this script to the _MainCamera GameObject along with
/// Deck, JsonParseDeck, and JsonParseLayout components.
/// Make sure JsonParseLayout references layout-Golf.json.
/// </summary>
[RequireComponent(typeof(Deck))]
[RequireComponent(typeof(JsonParseLayout))]
public class Golf : MonoBehaviour
{
    private static Golf S; // Singleton

    [Header("Dynamic")]
    public List<CardGolf> tableau;    // The 35 face-up cards in the grid
    public List<CardGolf> drawPile;   // Remaining face-down cards
    public List<CardGolf> discardPile;
    public CardGolf       target;     // Top of discard pile (current target card)

    private Transform     layoutAnchor;
    private Deck          deck;
    private JsonLayout    jsonLayout;

    // Maps layout slot id -> CardGolf for hiddenBy logic
    private Dictionary<int, CardGolf> slotIdToCard;

    void Start()
    {
        if (S != null) Debug.LogError("Golf S set more than once!");
        S = this;

        jsonLayout = GetComponent<JsonParseLayout>().layout;
        deck       = GetComponent<Deck>();
        deck.InitDeck();
        Deck.Shuffle(ref deck.cards);

        // Convert base Cards to CardGolf
        List<CardGolf> allCards = ConvertToCardGolf(deck.cards);

        // First 35 cards go to the tableau, rest to draw pile
        tableau   = new List<CardGolf>();
        drawPile  = new List<CardGolf>();
        discardPile = new List<CardGolf>();
        slotIdToCard = new Dictionary<int, CardGolf>();

        for (int i = 0; i < allCards.Count; i++)
        {
            if (i < jsonLayout.slots.Count)
                tableau.Add(allCards[i]);
            else
                drawPile.Add(allCards[i]);
        }

        LayoutTableau();
        LayoutDrawPile();

        // Flip first draw card as starting target
        MoveToTarget(DrawFromPile());
    }

    // -------------------------------------------------------------------------
    // Setup helpers
    // -------------------------------------------------------------------------

    List<CardGolf> ConvertToCardGolf(List<Card> cards)
    {
        List<CardGolf> result = new List<CardGolf>();
        foreach (Card c in cards)
            result.Add(c as CardGolf);
        return result;
    }

    void LayoutTableau()
    {
        if (layoutAnchor == null)
        {
            GameObject go = new GameObject("_LayoutAnchor");
            layoutAnchor = go.transform;
        }

        for (int i = 0; i < tableau.Count; i++)
        {
            CardGolf cg = tableau[i];
            JsonLayoutSlot slot = jsonLayout.slots[i];

            cg.transform.SetParent(layoutAnchor);
            cg.layoutSlot = slot;
            cg.state = eGolfCardState.tableau;
            cg.faceUp = slot.faceUp;

            int z = int.Parse(slot.layer[slot.layer.Length - 1].ToString());
            cg.SetLocalPos(new Vector3(
                jsonLayout.multiplier.x * slot.x,
                jsonLayout.multiplier.y * slot.y,
                -z));

            cg.SetSpriteSortingLayer(slot.layer);
            slotIdToCard[slot.id] = cg;
        }

        // Apply hiddenBy face-up logic (same as Prospector)
        SetTableauFaceUps();
    }

    void LayoutDrawPile()
    {
        for (int i = 0; i < drawPile.Count; i++)
        {
            CardGolf cg = drawPile[i];
            cg.transform.SetParent(layoutAnchor);
            cg.state = eGolfCardState.drawpile;
            cg.faceUp = false;

            Vector3 pos = new Vector3(
                jsonLayout.multiplier.x * jsonLayout.drawPile.x + jsonLayout.drawPile.xStagger * i,
                jsonLayout.multiplier.y * jsonLayout.drawPile.y,
                0.1f * i);
            cg.SetLocalPos(pos);
            cg.SetSpriteSortingLayer(jsonLayout.drawPile.layer);
            cg.SetSortingOrder(-10 * i);
        }
    }

    // -------------------------------------------------------------------------
    // Game actions
    // -------------------------------------------------------------------------

    CardGolf DrawFromPile()
    {
        if (drawPile.Count == 0) return null;
        CardGolf cg = drawPile[0];
        drawPile.RemoveAt(0);
        return cg;
    }

    void MoveToTarget(CardGolf cg)
    {
        if (cg == null) return;

        if (target != null)
        {
            // Send old target to discard
            target.state = eGolfCardState.discard;
            discardPile.Add(target);
            target.SetLocalPos(new Vector3(
                jsonLayout.multiplier.x * jsonLayout.discardPile.x,
                jsonLayout.multiplier.y * jsonLayout.discardPile.y,
                0));
            target.faceUp = true;
            target.SetSpriteSortingLayer(jsonLayout.discardPile.layer);
            target.SetSortingOrder(-200 + discardPile.Count * 3);
        }

        target = cg;
        cg.state = eGolfCardState.target;
        cg.faceUp = true;
        cg.SetLocalPos(new Vector3(
            jsonLayout.multiplier.x * jsonLayout.discardPile.x,
            jsonLayout.multiplier.y * jsonLayout.discardPile.y,
            0));
        cg.SetSpriteSortingLayer("Target");
        cg.SetSortingOrder(0);
    }

    void SetTableauFaceUps()
    {
        foreach (CardGolf cg in tableau)
        {
            bool faceUp = true;
            foreach (int coverID in cg.layoutSlot.hiddenBy)
            {
                if (slotIdToCard.ContainsKey(coverID))
                {
                    CardGolf cover = slotIdToCard[coverID];
                    if (cover != null && cover.state == eGolfCardState.tableau)
                        faceUp = false;
                }
            }
            cg.faceUp = faceUp;
        }
    }

    void CheckWinLose()
    {
        // Win: all tableau cards removed
        if (tableau.Count == 0)
        {
            Debug.Log("YOU WIN!");
            // Reload scene to play again
            SceneManager.LoadScene("__Prospector_Scene_0");
            return;
        }
        // Lose: draw pile empty and no valid moves
        if (drawPile.Count == 0)
        {
            bool anyMove = false;
            foreach (CardGolf cg in tableau)
            {
                if (cg.faceUp && cg.AdjacentTo(target))
                {
                    anyMove = true;
                    break;
                }
            }
            if (!anyMove)
            {
                Debug.Log("GAME OVER - No moves left!");
                SceneManager.LoadScene("__Prospector_Scene_0");
            }
        }
    }

    // -------------------------------------------------------------------------
    // Static click handler (called from CardGolf.OnMouseUpAsButton)
    // -------------------------------------------------------------------------

    static public void CARD_CLICKED(CardGolf cg)
    {
        switch (cg.state)
        {
            case eGolfCardState.target:
                // Clicking the target does nothing
                break;

            case eGolfCardState.drawpile:
                // Draw the next card and make it the target
                CardGolf drawn = S.DrawFromPile();
                if (drawn != null)
                {
                    S.MoveToTarget(drawn);
                    S.LayoutDrawPile(); // Re-stack the draw pile visually
                }
                break;

            case eGolfCardState.tableau:
                // Only valid if card is face-up and adjacent in rank to target
                if (!cg.faceUp) return;
                if (!cg.AdjacentTo(S.target)) return;

                // Remove from tableau and make it the new target
                S.tableau.Remove(cg);
                S.slotIdToCard.Remove(cg.layoutSlot.id);
                S.MoveToTarget(cg);
                S.SetTableauFaceUps(); // Reveal newly uncovered cards
                S.CheckWinLose();
                break;
        }
    }
}
