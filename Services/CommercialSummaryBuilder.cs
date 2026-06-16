using System;
using System.Text;

namespace Recordaudio.Services
{
    public sealed class CommercialSummaryBuilder
    {
        public string Build(string anonymizedTranscript)
        {
            if (string.IsNullOrWhiteSpace(anonymizedTranscript))
            {
                return "Aucun transcript disponible pour générer le résumé.";
            }

            var sb = new StringBuilder();

            sb.AppendLine("=== SYNTHÈSE COMMERCIALE ===");
            sb.AppendLine();
            sb.AppendLine("1. Activité / contexte");
            sb.AppendLine("- À compléter à partir du transcript.");
            sb.AppendLine();
            sb.AppendLine("2. Circuit de vente");
            sb.AppendLine("- À identifier : devis → facture / facture directe / mix.");
            sb.AppendLine();
            sb.AppendLine("3. Outils actuels");
            sb.AppendLine("- À relever dans le transcript : devis, facturation, achats, suivi paiements.");
            sb.AppendLine();
            sb.AppendLine("4. Répartition clients / typologie");
            sb.AppendLine("- À relever : B2B / B2C / B2G / mix.");
            sb.AppendLine();
            sb.AppendLine("5. Volumétrie");
            sb.AppendLine("- À relever : nombre de devis / factures par mois.");
            sb.AppendLine();
            sb.AppendLine("6. Organisation");
            sb.AppendLine("- À relever : nombre d'utilisateurs / gestion seul ou à plusieurs.");
            sb.AppendLine();
            sb.AppendLine("7. Irritants / douleurs");
            sb.AppendLine("- À extraire : temps perdu, erreurs, relances, paiements, réception achats.");
            sb.AppendLine();
            sb.AppendLine("8. Impacts");
            sb.AppendLine("- À identifier : temps, trésorerie, organisation, relation client.");
            sb.AppendLine();
            sb.AppendLine("9. Maturité facturation électronique");
            sb.AppendLine("- À identifier : niveau de compréhension / inquiétudes / anticipation.");
            sb.AppendLine();
            sb.AppendLine("10. Besoin cible");
            sb.AppendLine("- À reformuler : solution simple / rapide ou plus complète / structurante.");
            sb.AppendLine();
            sb.AppendLine("11. Prochaines étapes");
            sb.AppendLine("- À compléter selon l'échange.");
            sb.AppendLine();
            sb.AppendLine("=== BLOC AGILITY PRÉPARATOIRE ===");
            sb.AppendLine("- Client : CLIENT");
            sb.AppendLine("- Besoin principal : à préciser");
            sb.AppendLine("- Outils en place : à préciser");
            sb.AppendLine("- Douleurs : à préciser");
            sb.AppendLine("- Orientation solution : à préciser");
            sb.AppendLine("- Next step : à préciser");
            sb.AppendLine();
            sb.AppendLine("=== TRANSCRIPT ANONYMISÉ SOURCE ===");
            sb.AppendLine();
            sb.AppendLine(anonymizedTranscript);

            return sb.ToString().Trim();
        }
    }
}