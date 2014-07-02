﻿using HREngine.API;
using HREngine.API.Utilities;
using System;
using System.Collections.Generic;

namespace HREngine.Bots
{

   public abstract class Bot : API.IBot
   {
       private int concedeLvl = 5; // the rank, till you want to concede
       private int dirtytarget = -1;
       private int dirtychoice = -1;
       private string choiceCardId = "";
       Silverfish sf;

       public Bot()
       {
           OnBattleStateUpdate = HandleOnBattleStateUpdate;
           OnMulliganStateUpdate = HandleBattleMulliganPhase;
           bool concede = false;
           bool writeToSingleFile = false;
           try
           {
               concede = (HRSettings.Get.ReadSetting("silverfish.xml", "uai.autoconcede") == "true") ? true : false;
               writeToSingleFile = (HRSettings.Get.ReadSetting("silverfish.xml", "uai.singleLog") == "true") ? true : false;
           }
           catch
           {
               Helpfunctions.Instance.ErrorLog("a wild error occurrs! cant read the settings...");
           }
           try
           {
               this.concedeLvl = Convert.ToInt32((HRSettings.Get.ReadSetting("silverfish.xml", "uai.concedelvl")));
               if (this.concedeLvl >= 20) this.concedeLvl = 20;
               if (concede)
               {
                   Helpfunctions.Instance.ErrorLog("concede till rank " + concedeLvl);
               }
           }
           catch
           {
               Helpfunctions.Instance.ErrorLog("cant read your concede-Lvl");
           }

           this.sf = new Silverfish(writeToSingleFile);





           Mulligan.Instance.setAutoConcede(concede);

           sf.setnewLoggFile();

           try
           {
               int enfacehp = Convert.ToInt32((HRSettings.Get.ReadSetting("silverfish.xml", "uai.enemyfacehp")));
               Helpfunctions.Instance.ErrorLog("set enemy-face-hp to: " + enfacehp);
               ComboBreaker.Instance.attackFaceHP = enfacehp;
           }
           catch
           {
               Helpfunctions.Instance.ErrorLog("error in reading enemy-face-hp");
           }

           try
           {
               int mxwde = Convert.ToInt32((HRSettings.Get.ReadSetting("silverfish.xml", "uai.maxwide")));
               if (mxwde != 3000)
               {
                   Ai.Instance.setMaxWide(mxwde);
                   Helpfunctions.Instance.ErrorLog("set maxwide to: " + mxwde);
               }
           }
           catch
           {
               Helpfunctions.Instance.ErrorLog("error in reading Maxwide from settings, please recheck the entry");
           }

           try
           {
               bool twots = (HRSettings.Get.ReadSetting("silverfish.xml", "uai.simulateTwoTurns") == "true") ? true : false;
               if (twots)
               {
                   Ai.Instance.setTwoTurnSimulation(twots);
                   Helpfunctions.Instance.ErrorLog("activated two turn simulation");
               }

           }
           catch
           {
               Helpfunctions.Instance.ErrorLog("error in reading two-turn-simulation from settings");
           }

           Helpfunctions.Instance.ErrorLog("write to single log file is: " + writeToSingleFile);

           bool teststuff = false;
           bool printstuff = false;
           try
           {

               printstuff = (HRSettings.Get.ReadSetting("silverfish.xml", "uai.longteststuff") == "true") ? true : false;
               teststuff = (HRSettings.Get.ReadSetting("silverfish.xml", "uai.teststuff") == "true") ? true : false;
           }
           catch
           {
               Helpfunctions.Instance.ErrorLog("something went wrong with simulating stuff!");
           }

           if (teststuff)
           {
               Ai.Instance.autoTester(this, printstuff);
           }
       }


      private void concede()
      {
          /*int totalwin = 0;
          int totallose = 0;
          string[] lines = new string[0] { };
          try
          {
              string path = (HRSettings.Get.CustomRuleFilePath).Remove(HRSettings.Get.CustomRuleFilePath.Length - 13) + "Common" + System.IO.Path.DirectorySeparatorChar;
              lines = System.IO.File.ReadAllLines(path + "Settings.ini");
          }
          catch
          {
              Helpfunctions.Instance.logg("cant find Settings.ini");
          }
          foreach (string s in lines)
          {
              if (s.Contains("bot.stats.victory"))
              {
                  int val1 = s.Length;
                  string temp1 = s.Substring(18, (val1 - 18));
                  Helpfunctions.Instance.ErrorLog(temp1);
                  totalwin = int.Parse(temp1);
              }
              else if (s.Contains("bot.stats.defeat"))
              {
                  int val2 = s.Length;
                  string temp2 = s.Substring(17, (val2 - 17));
                  Helpfunctions.Instance.ErrorLog(temp2);
                  totallose = int.Parse(temp2);
              }
          }
          if (totalwin > totallose)
          {
              Helpfunctions.Instance.ErrorLog("not today!");
              HRGame.ConcedeGame();
          }*/
          int curlvl = HRPlayer.GetLocalPlayer().GetRank();
          if (HREngine.API.Utilities.HRSettings.Get.SelectedGameMode != HRGameMode.RANKED_PLAY) return;
          if(curlvl  < this.concedeLvl)
          {
                Helpfunctions.Instance.ErrorLog("not today!");
              HRGame.ConcedeGame();
          }
      }



      private HREngine.API.Actions.ActionBase HandleBattleMulliganPhase()
      {
          Helpfunctions.Instance.ErrorLog("handle mulligan");

          if ((TAG_MULLIGAN)HRPlayer.GetLocalPlayer().GetTag(HRGameTag.MULLIGAN_STATE) != TAG_MULLIGAN.INPUT)
          {
              Helpfunctions.Instance.ErrorLog("but we have to wait :D");
              return null;
          }

          if (HRMulligan.IsMulliganActive())
         {
            var list = HRCard.GetCards(HRPlayer.GetLocalPlayer(), HRCardZone.HAND);
            if (Mulligan.Instance.hasmulliganrules())
            {
                HRPlayer enemyPlayer = HRPlayer.GetEnemyPlayer();
                string enemName = Hrtprozis.Instance.heroIDtoName(enemyPlayer.GetHeroCard().GetEntity().GetCardId());
                List<Mulligan.CardIDEntity> celist= new List<Mulligan.CardIDEntity>();
                foreach (var item in list)
                {
                    if (item.GetEntity().GetCardId() != "GAME_005")// dont mulligan coin
                    {
                        celist.Add(new Mulligan.CardIDEntity(item.GetEntity().GetCardId(), item.GetEntity().GetEntityId()));
                    }
                }
                List<int> mullientitys = Mulligan.Instance.whatShouldIMulligan(celist, enemName);
                foreach (var item in list)
                {
                    if(mullientitys.Contains(item.GetEntity().GetEntityId()))
                    {
                        Helpfunctions.Instance.ErrorLog("Rejecting Mulligan Card " + item.GetEntity().GetName() + " because of your rules");
                        HRMulligan.ToggleCard(item);
                    }
                }


            }
            else
            {
                foreach (var item in list)
                {
                    if (item.GetEntity().GetCost() >= 4)
                    {
                        Helpfunctions.Instance.ErrorLog("Rejecting Mulligan Card " + item.GetEntity().GetName() + " because it cost is >= 4.");
                        HRMulligan.ToggleCard(item);
                    }
                    if (item.GetEntity().GetCardId() == "EX1_308" || item.GetEntity().GetCardId() == "EX1_622" || item.GetEntity().GetCardId() == "EX1_005")
                    {
                        Helpfunctions.Instance.ErrorLog("Rejecting Mulligan Card " + item.GetEntity().GetName() + " because it is soulfire or shadow word: death");
                        HRMulligan.ToggleCard(item);
                    }
                }
            }

            
            sf.setnewLoggFile();

            if (Mulligan.Instance.loserLoserLoser)
            {
                concede();
            }
            return null;
            //HRMulligan.EndMulligan();
         }
         return null;
      }

      /// <summary>
      /// [EN]
      /// This handler is executed when the local player turn is active.
      ///
      /// [DE]
      /// Dieses Event wird ausgelöst wenn der Spieler am Zug ist.
      /// </summary>
      private HREngine.API.Actions.ActionBase HandleOnBattleStateUpdate()
      {
          
          try
         {
             if (HRBattle.IsInTargetMode() && dirtytarget >= 0)
             {
                 Helpfunctions.Instance.ErrorLog("dirty targeting...");
                 HREntity target = getEntityWithNumber(dirtytarget);
                 
                 dirtytarget = -1;
                 
                 return new HREngine.API.Actions.TargetAction(target);
             }
             if (HRChoice.IsChoiceActive())
             {
                 if (this.dirtychoice >= 1)
                 {
                     List<HREntity> choices = HRChoice.GetChoiceCards();
                     int choice=this.dirtychoice;
                     this.dirtychoice=-1;
                     string ccId = this.choiceCardId;
                     this.choiceCardId = "";
                     HREntity target= choices[choice-1];
                     if (ccId == "EX1_160")
                     {
                         foreach (HREntity hre in choices)
                         {
                             if (choice == 1 && hre.GetCardId() == "EX1_160b") target = hre;
                             if (choice == 2 && hre.GetCardId() == "EX1_160a") target = hre;
                         }
                     }
                     if (ccId == "NEW1_008")
                     {
                         foreach (HREntity hre in choices)
                         {
                             if (choice == 1 && hre.GetCardId() == "NEW1_008a") target = hre;
                             if (choice == 2 && hre.GetCardId() == "NEW1_008b") target = hre;
                         }
                     }
                     if (ccId == "EX1_178")
                     {
                         foreach (HREntity hre in choices)
                         {
                             if (choice == 1 && hre.GetCardId() == "EX1_178a") target = hre;
                             if (choice == 2 && hre.GetCardId() == "EX1_178b") target = hre;
                         }
                     }
                     if (ccId == "EX1_573")
                     {
                         foreach (HREntity hre in choices)
                         {
                             if (choice == 1 && hre.GetCardId() == "EX1_573a") target = hre;
                             if (choice == 2 && hre.GetCardId() == "EX1_573b") target = hre;
                         }
                     }
                     if (ccId == "EX1_165")//druid of the claw
                     {
                         foreach (HREntity hre in choices)
                         {
                             if (choice == 1 && hre.GetCardId() == "EX1_165t1") target = hre;
                             if (choice == 2 && hre.GetCardId() == "EX1_165t2") target = hre;
                         }
                     }
                     if (ccId == "EX1_166")//keeper of the grove
                     {
                         foreach (HREntity hre in choices)
                         {
                             if (choice == 1 && hre.GetCardId() == "EX1_166a") target = hre;
                             if (choice == 2 && hre.GetCardId() == "EX1_166b") target = hre;
                         }
                     }
                     if (ccId == "EX1_155")
                     {
                         foreach (HREntity hre in choices)
                         {
                             if (choice == 1 && hre.GetCardId() == "EX1_155a") target = hre;
                             if (choice == 2 && hre.GetCardId() == "EX1_155b") target = hre;
                         }
                     }
                     if (ccId == "EX1_164")
                     {
                         foreach (HREntity hre in choices)
                         {
                             if (choice == 1 && hre.GetCardId() == "EX1_164a") target = hre;
                             if (choice == 2 && hre.GetCardId() == "EX1_164b") target = hre;
                         }
                     }
                     if (ccId == "New1_007")//starfall
                     {
                         foreach (HREntity hre in choices)
                         {
                             if (choice == 1 && hre.GetCardId() == "New1_007b") target = hre;
                             if (choice == 2 && hre.GetCardId() == "New1_007a") target = hre;
                         }
                     }
                     if (ccId == "EX1_154")//warth
                     {
                         foreach (HREntity hre in choices)
                         {
                             if (choice == 1 && hre.GetCardId() == "EX1_154a") target = hre;
                             if (choice == 2 && hre.GetCardId() == "EX1_154b") target = hre;
                         }
                     }
                     Helpfunctions.Instance.logg("chooses the card: " + target.GetCardId());
                     return new HREngine.API.Actions.ChoiceAction(target);
                 }
                 else
                 {
                     //Todo: ultimate tracking-simulation!
                     List<HREntity> choices = HRChoice.GetChoiceCards(); 
                     Random r = new Random();
                     int choice = r.Next(0,choices.Count);
                     Helpfunctions.Instance.logg("chooses a random card");
                     return new HREngine.API.Actions.ChoiceAction(choices[choice]);
                 }
             }
              
             sf.updateEverything(this);
            Action moveTodo = Ai.Instance.bestmove;
            if (moveTodo == null)
            {
                Helpfunctions.Instance.ErrorLog("end turn");
                return null;
            }
            Helpfunctions.Instance.ErrorLog("play action");
            moveTodo.print();
            if (moveTodo.cardplay)
            {
                HRCard cardtoplay = getCardWithNumber(moveTodo.cardEntitiy);
                if (moveTodo.enemytarget >= 0)
                {
                    HREntity target = getEntityWithNumber(moveTodo.enemyEntitiy);
                    Helpfunctions.Instance.ErrorLog("play: " + cardtoplay.GetEntity().GetName() + " target: " + target.GetName());
                    Helpfunctions.Instance.logg("play: " + cardtoplay.GetEntity().GetName() + " target: " + target.GetName() + " choice: " + moveTodo.druidchoice);
                    if (moveTodo.druidchoice >= 1)
                    {
                        if(moveTodo.enemyEntitiy>=0) this.dirtytarget = moveTodo.enemyEntitiy;
                        this.dirtychoice = moveTodo.druidchoice; //1=leftcard, 2= rightcard
                        this.choiceCardId = moveTodo.handcard.card.CardID;

                    }
                    if (moveTodo.handcard.card.type == CardDB.cardtype.MOB)
                    {
                        return new HREngine.API.Actions.PlayCardAction(cardtoplay, target, moveTodo.owntarget + 1);
                    }
                    
                    return new HREngine.API.Actions.PlayCardAction(cardtoplay,target);

                }
                else
                {
                    Helpfunctions.Instance.ErrorLog("play: " + cardtoplay.GetEntity().GetName() + " target nothing");
                    Helpfunctions.Instance.logg("play: " + cardtoplay.GetEntity().GetName() + " choice: " + moveTodo.druidchoice);
                    if (moveTodo.druidchoice >= 1)
                    {
                        this.dirtychoice = moveTodo.druidchoice; //1=leftcard, 2= rightcard
                        this.choiceCardId = moveTodo.handcard.card.CardID;

                    }
                    if (moveTodo.handcard.card.type == CardDB.cardtype.MOB)
                    {
                        return new HREngine.API.Actions.PlayCardAction(cardtoplay, null, moveTodo.owntarget + 1);
                    }
                    return new HREngine.API.Actions.PlayCardAction(cardtoplay);
                }
                
            }

            if (moveTodo.minionplay )
            {
                HREntity attacker = getEntityWithNumber(moveTodo.ownEntitiy);
                HREntity target = getEntityWithNumber(moveTodo.enemyEntitiy);
                Helpfunctions.Instance.ErrorLog("minion attack: " + attacker.GetName() + " target: " + target.GetName());
                Helpfunctions.Instance.logg("minion attack: " + attacker.GetName() + " target: " + target.GetName());
                return new HREngine.API.Actions.AttackAction(attacker,target);

            }

            if (moveTodo.heroattack)
            {
                HREntity attacker = getEntityWithNumber(moveTodo.ownEntitiy);
                HREntity target = getEntityWithNumber(moveTodo.enemyEntitiy);
                this.dirtytarget = moveTodo.enemyEntitiy;
                Helpfunctions.Instance.ErrorLog("heroattack: " + attacker.GetName() + " target: " + target.GetName());
                Helpfunctions.Instance.logg("heroattack: " + attacker.GetName() + " target: " + target.GetName());
                if (HRPlayer.GetLocalPlayer().HasWeapon() )
                {
                    Helpfunctions.Instance.ErrorLog("hero attack with weapon");
                    return new HREngine.API.Actions.AttackAction(HRPlayer.GetLocalPlayer().GetWeaponCard().GetEntity(), target);
                }
                Helpfunctions.Instance.ErrorLog("hero attack without weapon");
                //Helpfunctions.Instance.ErrorLog("attacker entity: " + HRPlayer.GetLocalPlayer().GetHero().GetEntityId());
                return new HREngine.API.Actions.AttackAction(HRPlayer.GetLocalPlayer().GetHero(), target);

            }

            if (moveTodo.useability)
            {
                HRCard cardtoplay = HRPlayer.GetLocalPlayer().GetHeroPower().GetCard();

                if (moveTodo.enemytarget >= 0)
                {
                    HREntity target = getEntityWithNumber(moveTodo.enemyEntitiy);
                    Helpfunctions.Instance.ErrorLog("use ablitiy: " + cardtoplay.GetEntity().GetName() + " target " + target.GetName());
                    Helpfunctions.Instance.logg("use ablitiy: " + cardtoplay.GetEntity().GetName() + " target " + target.GetName());
                    return new HREngine.API.Actions.PlayCardAction(cardtoplay, target);

                }
                else
                {
                    Helpfunctions.Instance.ErrorLog("use ablitiy: " + cardtoplay.GetEntity().GetName() + " target nothing");
                    Helpfunctions.Instance.logg("use ablitiy: " + cardtoplay.GetEntity().GetName() + " target nothing");
                    return new HREngine.API.Actions.PlayCardAction(cardtoplay);
                }
            }

         }
         catch (Exception Exception)
         {
             Helpfunctions.Instance.ErrorLog(Exception.Message);
             Helpfunctions.Instance.ErrorLog(Environment.StackTrace);
         }
         return null;
         //HRBattle.FinishRound();
      }

      private HREntity getEntityWithNumber(int number)
      {
          foreach (HREntity e in this.getallEntitys())
          {
              if (number == e.GetEntityId()) return e;
          }
          return null;
      }

      private HRCard getCardWithNumber(int number)
      {
          foreach (HRCard e in this.getallHandCards())
          {
              if (number == e.GetEntity().GetEntityId()) return e;
          }
          return null;
      }

      private List<HREntity> getallEntitys()
      {
          List<HREntity> result = new List<HREntity>();
          HREntity ownhero = HRPlayer.GetLocalPlayer().GetHero();
          HREntity enemyhero = HRPlayer.GetEnemyPlayer().GetHero();
          HREntity ownHeroAbility = HRPlayer.GetLocalPlayer().GetHeroPower();
          List<HRCard> list2 = HRCard.GetCards(HRPlayer.GetLocalPlayer(), HRCardZone.PLAY);
          List<HRCard> list3 = HRCard.GetCards(HRPlayer.GetEnemyPlayer(), HRCardZone.PLAY);

          result.Add(ownhero);
          result.Add(enemyhero);
          result.Add(ownHeroAbility);

          foreach (HRCard item in list2)
          {
              result.Add(item.GetEntity());
          }
          foreach (HRCard item in list3)
          {
              result.Add(item.GetEntity());
          }
          

          

          return result;
      }

      private List<HRCard> getallHandCards()
      {
          List<HRCard> list = HRCard.GetCards(HRPlayer.GetLocalPlayer(), HRCardZone.HAND);
          return list;
      }


      protected virtual HRCard GetMinionByPriority(HRCard lastMinion = null)
      {
         return null;
      }

      public int getPlayfieldValue(Playfield p)
      {
          return evaluatePlayfield(p);
      }

      public int getRulesEditorPenality(string cardId, Playfield p)
      {
          
          return 0;
      }

      protected virtual int evaluatePlayfield(Playfield p)
      {
          return 0;
      }
   
   }
}