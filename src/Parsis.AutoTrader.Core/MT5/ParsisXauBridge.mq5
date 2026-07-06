#property strict
#property version   "2.0"
#property description "Parsis AutoTrader automatic bridge"
#include <Trade/Trade.mqh>

input int PollMilliseconds = 250;
CTrade trade;
string root="ParsisAutoTrader\\";

int OnInit(){EventSetMillisecondTimer(MathMax(100,PollMilliseconds));FolderCreate(root+"commands",FILE_COMMON);FolderCreate(root+"done",FILE_COMMON);WriteStatus("Bridge started");return(INIT_SUCCEEDED);}
void OnDeinit(const int reason){EventKillTimer();}
void OnTimer(){ProcessCommands();ManagePositions();WriteStatus("Connected");}

void ProcessCommands(){
 string mask=root+"commands\\*.cmd"; string name=""; long h=FileFindFirst(mask,name,FILE_COMMON); if(h==INVALID_HANDLE)return;
 do{string path=root+"commands\\"+name;int f=FileOpen(path,FILE_READ|FILE_TXT|FILE_ANSI|FILE_COMMON);if(f!=INVALID_HANDLE){string line=FileReadString(f);FileClose(f);Execute(line);FileMove(path,0,root+"done\\"+name,FILE_COMMON|FILE_REWRITE);}}while(FileFindNext(h,name));FileFindClose(h);
}

void Execute(string line){
 string p[]; int n=StringSplit(line,'|',p); if(n<2)return; string id=p[0],action=p[1];
 if(action=="CLOSE_MAGIC" && n>=3){CloseMagic((long)StringToInteger(p[2]));return;}
 if(action=="DELETE_PENDING" && n>=3){DeletePending((long)StringToInteger(p[2]));return;}
 if(action!="OPEN"||n<14)return;
 string side=p[2],mode=p[3],symbol=p[4];double volume=StringToDouble(p[5]),lo=StringToDouble(p[6]),hi=StringToDouble(p[7]),sl=StringToDouble(p[8]),tp1=StringToDouble(p[9]),tp2=StringToDouble(p[10]),tp3=StringToDouble(p[11]);long magic=(long)StringToInteger(p[12]);int dev=(int)StringToInteger(p[13]);
 if(!SymbolSelect(symbol,true)){Ack(id,false,"Symbol not found");return;} trade.SetExpertMagicNumber(magic);trade.SetDeviationInPoints(dev); string comment="PTA:"+DoubleToString(tp1,_Digits)+":"+DoubleToString(tp2,_Digits)+":"+DoubleToString(tp3,_Digits)+":0";
 bool ok=false; double ask=SymbolInfoDouble(symbol,SYMBOL_ASK),bid=SymbolInfoDouble(symbol,SYMBOL_BID),entry=(lo+hi)/2.0;
 if(mode=="MARKET") ok=(side=="BUY")?trade.Buy(volume,symbol,0,sl,tp3,comment):trade.Sell(volume,symbol,0,sl,tp3,comment);
 else if(mode=="LIMIT") ok=(side=="BUY")?trade.BuyLimit(volume,entry,symbol,sl,tp3,ORDER_TIME_GTC,0,comment):trade.SellLimit(volume,entry,symbol,sl,tp3,ORDER_TIME_GTC,0,comment);
 else ok=(side=="BUY")?trade.BuyStop(volume,entry,symbol,sl,tp3,ORDER_TIME_GTC,0,comment):trade.SellStop(volume,entry,symbol,sl,tp3,ORDER_TIME_GTC,0,comment);
 Ack(id,ok,trade.ResultRetcodeDescription());
}

void ManagePositions(){
 for(int i=PositionsTotal()-1;i>=0;i--){ulong ticket=PositionGetTicket(i);if(ticket==0||!PositionSelectByTicket(ticket))continue;string c=PositionGetString(POSITION_COMMENT);if(StringFind(c,"PTA:")!=0)continue;string a[];if(StringSplit(c,':',a)<5)continue;double tp1=StringToDouble(a[1]),tp2=StringToDouble(a[2]),tp3=StringToDouble(a[3]);int stage=ReadStage(ticket);string sym=PositionGetString(POSITION_SYMBOL);long type=PositionGetInteger(POSITION_TYPE);double price=(type==POSITION_TYPE_BUY)?SymbolInfoDouble(sym,SYMBOL_BID):SymbolInfoDouble(sym,SYMBOL_ASK);double vol=PositionGetDouble(POSITION_VOLUME),step=SymbolInfoDouble(sym,SYMBOL_VOLUME_STEP),minv=SymbolInfoDouble(sym,SYMBOL_VOLUME_MIN),open=PositionGetDouble(POSITION_PRICE_OPEN);bool hit1=(type==POSITION_TYPE_BUY)?price>=tp1:price<=tp1;bool hit2=(type==POSITION_TYPE_BUY)?price>=tp2:price<=tp2;bool hit3=(type==POSITION_TYPE_BUY)?price>=tp3:price<=tp3;
  if(stage==0&&hit1){double close=NormVol(vol*0.5,step,minv);if(close<vol&&trade.PositionClosePartial(ticket,close)){trade.PositionModify(ticket,open,tp3);UpdateComment(ticket,tp1,tp2,tp3,1);}}
  else if(stage==1&&hit2){double close=NormVol(vol*0.5,step,minv);if(close<vol&&trade.PositionClosePartial(ticket,close))UpdateComment(ticket,tp1,tp2,tp3,2);}
  else if(hit3)trade.PositionClose(ticket);
 }
}

double NormVol(double v,double step,double minv){double x=MathFloor(v/step)*step;return NormalizeDouble(MathMax(minv,x),8);}
int ReadStage(ulong ticket){string f=root+"stage_"+(string)ticket+".txt";int h=FileOpen(f,FILE_READ|FILE_TXT|FILE_COMMON);if(h==INVALID_HANDLE)return 0;int s=(int)FileReadNumber(h);FileClose(h);return s;}
void UpdateComment(ulong ticket,double a,double b,double c,int stage){string f=root+"stage_"+(string)ticket+".txt";int h=FileOpen(f,FILE_WRITE|FILE_TXT|FILE_COMMON);if(h!=INVALID_HANDLE){FileWrite(h,stage);FileClose(h);}}
void CloseMagic(long magic){for(int i=PositionsTotal()-1;i>=0;i--){ulong t=PositionGetTicket(i);if(t>0&&PositionSelectByTicket(t)&&PositionGetInteger(POSITION_MAGIC)==magic)trade.PositionClose(t);}}
void DeletePending(long magic){for(int i=OrdersTotal()-1;i>=0;i--){ulong t=OrderGetTicket(i);if(t>0&&OrderGetInteger(ORDER_MAGIC)==magic)trade.OrderDelete(t);}}
void Ack(string id,bool ok,string msg){int h=FileOpen(root+"done\\"+id+".ack",FILE_WRITE|FILE_TXT|FILE_ANSI|FILE_COMMON);if(h!=INVALID_HANDLE){FileWrite(h,(ok?"OK|":"ERROR|")+msg);FileClose(h);}}
void WriteStatus(string msg){string path=root+"status.json";string tmp=root+"status.tmp";int h=FileOpen(tmp,FILE_WRITE|FILE_TXT|FILE_ANSI|FILE_COMMON);if(h==INVALID_HANDLE)return;string s=StringFormat("{\"Connected\":true,\"Login\":%I64d,\"Server\":\"%s\",\"Balance\":%.2f,\"Equity\":%.2f,\"Margin\":%.2f,\"FreeMargin\":%.2f,\"OpenPositions\":%d,\"Message\":\"%s\",\"Timestamp\":\"%s\"}",AccountInfoInteger(ACCOUNT_LOGIN),AccountInfoString(ACCOUNT_SERVER),AccountInfoDouble(ACCOUNT_BALANCE),AccountInfoDouble(ACCOUNT_EQUITY),AccountInfoDouble(ACCOUNT_MARGIN),AccountInfoDouble(ACCOUNT_MARGIN_FREE),PositionsTotal(),msg,TimeToString(TimeGMT(),TIME_DATE|TIME_SECONDS));FileWriteString(h,s);FileClose(h);FileMove(tmp,FILE_COMMON,path,FILE_COMMON|FILE_REWRITE);}
