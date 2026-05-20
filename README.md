# CyberBrass

A single-player cyberpunk & dieselpunk first-person shooter set in a brass-and-neon world, built in **Unity 6** with a brass-forward color palette and physics-based interactions.

---

## ⚡ Quick Start

### 1. Open the Project in Unity 6
1. Launch **Unity Hub** and click **Add** > **Add project from disk**.
2. Select the `cyber-brass` folder.
3. Open the project using **Unity 6 LTS**.

### 2. Generate the Testing Playground
To populate the scene with the physical environment, targets, and the player weapon model:
1. In the Unity Editor top menu, navigate to:  
   **`CyberBrass`** ➡️ **`Generate Playground Scene`**
2. This creates and loads the interactive blockout scene located at `Assets/Scenes/Playground.unity`.

### 3. Play the Game
Click the **Play** button at the top of the Unity Editor. Use the following control layout:

| Action | Bindings |
| :--- | :--- |
| **Move** | `W` `A` `S` `D` or **Arrow Keys** `↑` `↓` `←` `→` |
| **Jump** | `Left Shift` |
| **Shoot** | `Space`, **Left Mouse Click**, or `Left/Right Ctrl` |
| **Reload** | `R` |

*Aim slightly upward and fire to watch your physical amber grenades follow a gravity trajectory arc, blow up, and knock target cylinders back with radial explosion forces!*

### 4. Run Automated Integration Tests
Verify physics, inputs, weapon states, and FPS metrics:
1. Open the Test Runner window: **`Window`** ➡️ **`General`** ➡️ **`Test Runner`**.
2. Select the **`PlayMode`** tab.
3. Click **`Run All`** to execute the test suite. All tests should pass green.

---

## 🎨 Art & Design Reference
For look-development and visual styling guides mapping to Milestone 4, review the user-approved artwork specifications:
*   **[Weapon Design Reference](file:///C:/Users/User/.gemini/antigravity/brain/7d3f19e7-c7ec-407f-b3b7-943138a5f59c/weapon_design_reference.md)**: Visual styling analysis and first-person concept art for **"The Foreman"** revolver.
