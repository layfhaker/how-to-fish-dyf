# DYF (DEP YOUR FRIEND)

Gamble your dead friends on the casino roulette table in **How to Fish**!

---

## 🎲 Features

### 1. No Auto-Respawn in Co-op
When playing with 2+ players, a dead player **cannot give up and respawn on their own**.
A teammate must pay their bail or gamble them on the roulette wheel!
*(If all players die, the normal team wipe resets the boat).*

### 2. Friend's Value = a Debt (starts -$100)
A dead friend is a liability: their starting **value is -$100** — that's the **$100 debt** you must pay to revive them.
Hold LMB to slap and revive once your team balance can cover it.

### 3. Casino Roulette Betting (Multiply / Divide)
Carry the corpse to the roulette table and place it in the betting box. Each spin changes the friend's value:

* **Red / Black (×2)** and **Green (×35):**
  * **WIN:** if it's a debt it **halves** (shrinks toward $0); if it's already a profit it **doubles**.
  * **LOSS:** if it's a debt it **doubles**; if it's a profit it **halves**.
* **Buy-out line ($99):** when the magnitude reaches **$99 or less**, the next favorable result **flips the sign** — the friend goes from debt to profit (and back).
* The corpse is **NEVER destroyed on loss** — keep spinning as long as you dare.
* **Reviving:** debt → the team bank **pays** `|value|`; profit → the bank **earns** `value`; `$0` → free.

A fixed **2D on-screen HUD** shows the friend's current debt/profit whenever you hover or carry a corpse, and the casino table shows the signed value (don't get confused by that minus!).

### 4. 👻 Spooky Dead Voice Chat
While dead, the player speaks directly from their corpse on the table with:
* Deep demonic pitch (`0.80x`).
* Muffled coffin acoustic filter (`AudioLowPassFilter`).
* Ghostly echo & cave reverberation (`AudioEchoFilter` + `AudioReverbFilter`).