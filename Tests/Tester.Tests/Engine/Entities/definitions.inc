<!ENTITY DATETIMEMS "\d{8}-\d{2}:\d{2}:\d{2}(\.\d{3})?">
<!ENTITY DATETIMEWITHOUTMS "\d{8}-\d{2}:\d{2}:\d{2}(\.\d{3})?">
<!ENTITY STRING "[\w\s\p{P}]*">

<!ENTITY ME "Tester">
<!ENTITY YOU "Engine">
<!ENTITY THROW_TIMES "1">

<!ENTITY STD_LOGGER "Epam.FixAntenna.Tester.Logger.LoggingCaseLogger">
<!ENTITY LAZY_UPDATER "Epam.FixAntenna.Tester.Updater.LazyUpdater">
<!ENTITY REGEXP_UNORDERED "Epam.FixAntenna.Tester.Comparator.RegExpFlexibleComparator">
<!-- acceptor setup and engine connection string -->
<!ENTITY PORT "3001">
<!ENTITY FIX_VERSION "FIX.4.2">
<!ENTITY HOST "localhost">

<!ENTITY ENGINE_PORT "3000">
<!ENTITY ENGINE_HOST "localhost">

<!-- Utility stuff -->
<!ENTITY CLEAN_LOGS SYSTEM "Util/cleanLogs.xml">
<!ENTITY ACCEPTOR_TRANSPORT_UP SYSTEM "Util/acceptorTransport.xml">
<!ENTITY INITIATOR_TRANSPORT_UP SYSTEM "Util/initiatorTransport.xml">
<!ENTITY RECEIVE_LOGON SYSTEM "Util/receiveLogon.xml">
<!ENTITY SEND_LOGON SYSTEM "Util/sendLogon.xml">

<!-- Engine specific stuff -->
<!ENTITY ENGINE_UP SYSTEM "System/engineUp.xml">
<!ENTITY ENGINE_UP_WITH_APP SYSTEM "System/engineUpWithApp.xml">
<!ENTITY ENGINE_UP_WITH_APP_FOR_CME SYSTEM "System/engineUpWithApp_For_RR.xml">
<!ENTITY ENGINE_UP_WITH_APP_MAX_DIFF SYSTEM "System/engineUpWithApp_For_RR_With_MaxDiff.xml">
<!ENTITY ENGINE_UP_WITH_ECHO_APP SYSTEM "System/engineUpWithEchoApp.xml">
<!ENTITY REJECTION_ENGINE_UP SYSTEM "System/rejectingEngineUp.xml">
<!ENTITY ENGINE_DOWN SYSTEM "System/engineDown.xml">
<!ENTITY SESSION_UP SYSTEM "System/sessionUp.xml">
<!ENTITY SESSION_UP_WITH_ERROR SYSTEM "System/sessionUpWithThrowError.xml">
<!ENTITY SESSION_DOWN SYSTEM "System/sessionDown.xml">
<!ENTITY SESSION_WAIT_DOWN SYSTEM "System/sessionWaitForDown.xml">
<!ENTITY SEND_MESSAGE SYSTEM "System/sendMessage.xml">
