using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enum for the possible states of a CardGolf card during gameplay.
/// </summary>
public enum eGolfCardState
{
    tableau,   // Card is on the 7x5 tableau grid
    drawpile,  // Card is in the face-down draw pile
    target,    // Card is the current top of the discard pile (target)
    discard    // Card has been discarded / played off the tableau
}

/// <summary>
/// CardGolf extends the base Card class with Golf Solitaire-specific
/// state tracking and click handling.
///
/// Golf Solitaire Rules:
///   - 35 cards laid out face-up in 7 columns of 5 cards each.
///   - 17 remaining cards form the draw pile (face-down).
///   - One card is flipped face-up as the starting target.
///   - On each turn, click any BOTTOM ROW card from the tableau
///     that is one rank above OR below the current target card.
///   - Suit does not matter. Ace and King DO wrap (A-K are adjacent).
///   - If no moves are available, click the draw pile to flip a new target.
///   - Win by clearing all 35 tableau cards.
///   - Lose if the draw pile runs out and no moves remain.
/// </summary>
public class CardGolf : Card
{
    [Header("Dynamic - Golf")]
    public eGolfCardState state = eGolfCardState.tableau;

    // Index of this card's column in the tableau (0 = leftmost, 6 = rightmost)
    public int columnIndex = -1;

    // Index of this card's row in its column (0 = top/back, 4 = bottom/front)
    public int rowIndex = -1;

    // The layout slot info assigned when the tableau is built
    public JsonLayoutSlot layoutSlot;

    /// <summary>
    /// Called by Unity when this card is clicked.
    /// Forwards the click to the Golf game manager singleton.
    /// </summary>
    public override void OnMouseUpAsButton()
    {
        Golf.CARD_CLICKED(this);
    }
}
