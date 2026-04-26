# 🚴 Cyclist Simulation — Projet 2

**Équipe :** MAWANDU HAMBA Héritier · Landry IRUMVA · Berthé KADIDIATOU
**Cours :** Réalité Virtuelle

---

## 🎯 Description du projet

Simulation urbaine interactive sous **Unity** où un joueur pilote un vélo au cœur d'une ville animée par des cyclistes autonomes (IA). L'IA détecte les obstacles et réagit dynamiquement (ralentissement, esquive, arrêt d'urgence).

---

## 📦 Packages requis (à importer manuellement dans Unity)

> ⚠️ Ces assets volumineux ne sont **pas** inclus dans ce dépôt. Chaque membre doit les importer.

| Package | Rôle |
|---|---|
| **Easy Bike System** | Physique & animations du vélo joueur |
| **Urban Traffic & Pedestrian System (UTS)** | Trafic IA, piétons, génération des routes |
| **Population System Pro** | Gestion de la foule et des agents |

---

## 🗂️ Structure du code (fichiers créés par notre équipe)

```
Assets/
├── Scripts/
│   └── AIBikeObstacleAvoidance.cs   ← IA d'esquive (SphereCast, déviation, arrêt d'urgence)
├── Editor/
│   └── CyclisteSetupTools.cs        ← Menu Unity "Projet 2" (Options 1, 2, 3)
└── UTS_FullPack/ (modifié)
    └── Editor/UTS/
        ├── UTS_AudiencePathEditor.cs ← Fix raycasting souris (API moderne)
        ├── BcycleGyroEditor.cs       ← Fix raycasting souris
        ├── CarWalkPathEditor.cs      ← Fix raycasting souris
        └── UTS_PeopleWalkPathEditor.cs ← Fix raycasting souris
```

---

## 🚀 Comment lancer la simulation

### Étape 1 — Importer les packages
Importez les 3 assets Unity listés ci-dessus dans votre projet.

### Étape 2 — Ouvrir la scène
Ouvrez la scène `Assets/UTS_FullPack/Scenes/Cars_Gyro_Bikes_Peoples1.unity` ou créez une scène vide avec un `Plane`.

### Étape 3 — Utiliser le menu « Projet 2 »
| Option | Action |
|---|---|
| **1. Setup Player Bicycle** | Fait apparaître votre vélo jouable sur la route |
| **2. Setup AI Bicycle Path** | Crée un réseau de routes personnalisé (tracé à la souris) |
| **3. Complete Triggers & Obstacles** | Greffe le cerveau d'esquive sur tous les cyclistes IA |

### Étape 4 — Jouer
Appuyez sur **Play**. Utilisez les touches directionnelles pour conduire.

---

## 🧠 Architecture technique — Comportement IA

```
Détection SphereCast (2m rayon)
        │
        ▼
┌──────────────────────────────┐
│  Obstacle détecté ?          │
│  Oui → Distance > 4m ?       │
│         Oui → Ralentir +     │
│              Esquive Lerp    │
│              ClampMag(2.5m)  │
│         Non → Arrêt d'urgence│
│              Velocity = 0    │
│  Non → Reprendre vitesse     │
└──────────────────────────────┘
```

---

## 🐛 Difficultés résolues

| Problème | Solution |
|---|---|
| `CS0101` — Classes dupliquées entre packages | Renommage complet avec préfixe `UTS_` |
| Clics souris ignorés dans l'éditeur UTS | Remplacement de `Camera.ScreenPointToRay` → `HandleUtility.GUIPointToWorldRay` |
| Vélo IA sortant de la route lors des esquives | Ajout d'un `Vector3.ClampMagnitude(2.5f)` pour limiter la déviation |
