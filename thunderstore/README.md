# DYF (DEP YOUR FRIEND)

Gamble your dead friends on the casino roulette table in **How to Fish**!

---

## 🎲 Features

### 1. No Auto-Respawn in Co-op
When playing with 2+ players, a dead player **cannot give up and respawn on their own**. 
A teammate must pay their bail or gamble them on the roulette wheel!
*(If all players die, normal team wipe resets the boat).*

### 2. Base Bail ($100)
Initial ransom/bail for reviving a dead friend is **$100**. Hold LMB to slap and revive them once you have enough funds in your team balance.

### 3. Casino Roulette Betting
Carry your friend's corpse to the casino roulette table and place them in the betting box:
* **Red / Black:**
  * **Win:** Bail decreases by **-$100** ($100 ➔ $0 [Free!] ➔ -$100 [Profit!]).
  * **Loss:** Bail increases by **+$100** ($100 ➔ $200 ➔ $300...).
* **Green (Zero):**
  * **Win:** Bail decreases by **-$3300**!
  * **Loss:** Bail increases by **+$3300**!
* **The corpse is NEVER destroyed on loss!** You can keep spinning as many times as you dare.
* If bail goes negative (huge win streak), reviving the friend **deposits all profit straight into your team bank**!

### 4. 👻 Spooky Dead Voice Chat
While dead, the player speaks directly from their corpse on the table with:
* Deep demonic pitch (`0.80x`).
* Muffled coffin acoustic filter (`AudioLowPassFilter`).
* Ghostly echo & cave reverberation (`AudioEchoFilter` + `AudioReverbFilter`).

---

## 🎮 Solo Testing Hotkeys (Debug Mode)
* **F6**: Give yourself +$500 to team bank.
* **F7**: Spawn a dummy dead friend corpse in front of you (ready to carry to the casino!).
* **F8**: Toggle Solo Test Mode (enables death-lock in singleplayer).
* **F9**: Suicide (test death screen in singleplayer).
* **F10**: Revive yourself.
