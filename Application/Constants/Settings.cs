// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants
{
    public class Settings
    {
        public const string APP_NAME = "APP_NAME";
        public const string APP_ADDRESS_NAME = "APP_ADDRESS_NAME";
        public const string APP_ADDRESS_SUPPLEMENT = "APP_ADDRESS_SUPPLEMENT";
        public const string APP_ADDRESS_ADDRESS = "APP_ADDRESS_ADDRESS";
        public const string APP_ADDRESS_ZIP = "APP_ADDRESS_ZIP";
        public const string APP_ADDRESS_PLACE = "APP_ADDRESS_PLACE";
        public const string APP_ADDRESS_STATE = "APP_ADDRESS_STATE";
        public const string APP_ADDRESS_COUNTRY = "APP_ADDRESS_COUNTRY";
        public const string APP_ADDRESS_PHONE = "APP_ADDRESS_PHONE";
        public const string APP_ADDRESS_MAIL = "APP_ADDRESS_MAIL";

        public const string APP_AUTHENTICATION_TYPE = "authenticationType";
        public const string APP_ENABLE_SSL = "enabledSSL";
        public const string APP_DISPOSITION_NOTIFICATION = "dispositionNotification";
        public const string APP_REPLY_TO = "replyTo";
        public const string APP_OUTGOING_SERVER = "outgoingserver";
        public const string APP_OUTGOING_SERVER_TIMEOUT = "outgoingserverTimeout";
        public const string APP_READ_RECEIPT = "readReceipt";
        public const string APP_MARK = "mark";
        public const string APP_OUTGOING_SERVER_PORT = "outgoingserverPort";
        public const string APP_EMAIL_ADDRESS = "APP_ADDRESS_MAIL";
        public const string APP_SUBJECT = "subject";

        public const string APP_OUTGOING_SERVER_USERNAME = "outgoingserverUsername";
        public const string APP_OUTGOING_SERVER_PASSWORD = "outgoingserverPassword";

        public const string SCHEDULING_MAX_WORK_DAYS = "SCHEDULING_MAX_WORK_DAYS";
        public const string SCHEDULING_MIN_REST_DAYS = "SCHEDULING_MIN_REST_DAYS";
        public const string SCHEDULING_MIN_PAUSE_HOURS = "SCHEDULING_MIN_PAUSE_HOURS";
        public const string SCHEDULING_MAX_OPTIMAL_GAP = "SCHEDULING_MAX_OPTIMAL_GAP";
        public const string SCHEDULING_MAX_DAILY_HOURS = "SCHEDULING_MAX_DAILY_HOURS";
        public const string SCHEDULING_MAX_WEEKLY_HOURS = "SCHEDULING_MAX_WEEKLY_HOURS";
        public const string SCHEDULING_MAX_CONSECUTIVE_DAYS = "SCHEDULING_MAX_CONSECUTIVE_DAYS";

        public const string HEARTBEAT_ENABLED_GLOBALLY = "HEARTBEAT_ENABLED_GLOBALLY";
        public const string HEARTBEAT_DEFAULT_INTERVAL = "HEARTBEAT_DEFAULT_INTERVAL";

        public const string REPORT_DEFAULT_TEMPLATES = "REPORT_DEFAULT_TEMPLATES";

        public const string SCHEDULE_EMAIL_SUBJECT_TEMPLATE = "SCHEDULE_EMAIL_SUBJECT_TEMPLATE";
        public const string SCHEDULE_EMAIL_BODY_TEMPLATE = "SCHEDULE_EMAIL_BODY_TEMPLATE";

        public const string PROMPT_TRANSLATIONS_PREFIX = "PROMPT_TRANSLATIONS_";

        public const string LLM_FALLBACK_MODEL_ID = "claude-haiku-4-5-20251001";

        public const string APP_INCOMING_SERVER = "incomingserver";
        public const string APP_INCOMING_SERVER_PORT = "incomingserverPort";
        public const string APP_INCOMING_SERVER_USERNAME = "incomingserverUsername";
        public const string APP_INCOMING_SERVER_PASSWORD = "incomingserverPassword";
        public const string APP_INCOMING_SERVER_SSL = "incomingserverSSL";
        public const string APP_INCOMING_SERVER_FOLDER = "incomingserverFolder";
        public const string APP_INCOMING_SERVER_POLL_INTERVAL = "incomingserverPollInterval";

        public const string WEB_SEARCH_PROVIDER = "WEB_SEARCH_PROVIDER";
        public const string WEB_SEARCH_API_KEY = "WEB_SEARCH_API_KEY";
        public const string WEB_SEARCH_MAX_RESULTS = "WEB_SEARCH_MAX_RESULTS";

        public const string SPAM_FILTER_SPAM_THRESHOLD = "SPAM_FILTER_SPAM_THRESHOLD";
        public const string SPAM_FILTER_UNCERTAIN_THRESHOLD = "SPAM_FILTER_UNCERTAIN_THRESHOLD";
        public const string SPAM_FILTER_LLM_ENABLED = "SPAM_FILTER_LLM_ENABLED";

        public const string DEEPL_API_KEY = "DEEPL_API_KEY";
        public const string OPENROUTESERVICE_API_KEY = "OPENROUTESERVICE_API_KEY";

        public const string ROUTE_MIN_TRAVEL_TIME_BY_CAR = "ROUTE_MIN_TRAVEL_TIME_BY_CAR";
        public const string ROUTE_MIN_TRAVEL_TIME_BY_BICYCLE = "ROUTE_MIN_TRAVEL_TIME_BY_BICYCLE";
        public const string ROUTE_MIN_TRAVEL_TIME_BY_FOOT = "ROUTE_MIN_TRAVEL_TIME_BY_FOOT";

        public const string INSTALLED_LANGUAGE_PREFIX = "INSTALLED_LANGUAGE_";

        public const string COMPANY_TAX_ID = "COMPANY_TAX_ID";
        public const string COMPANY_VAT_ID = "COMPANY_VAT_ID";
        public const string COMPANY_COMMERCIAL_REGISTER = "COMPANY_COMMERCIAL_REGISTER";

        public const string SCHEDULING_DEFAULT_WORK_ON_MONDAY = "SCHEDULING_DEFAULT_WORK_ON_MONDAY";
        public const string SCHEDULING_DEFAULT_WORK_ON_TUESDAY = "SCHEDULING_DEFAULT_WORK_ON_TUESDAY";
        public const string SCHEDULING_DEFAULT_WORK_ON_WEDNESDAY = "SCHEDULING_DEFAULT_WORK_ON_WEDNESDAY";
        public const string SCHEDULING_DEFAULT_WORK_ON_THURSDAY = "SCHEDULING_DEFAULT_WORK_ON_THURSDAY";
        public const string SCHEDULING_DEFAULT_WORK_ON_FRIDAY = "SCHEDULING_DEFAULT_WORK_ON_FRIDAY";
        public const string SCHEDULING_DEFAULT_WORK_ON_SATURDAY = "SCHEDULING_DEFAULT_WORK_ON_SATURDAY";
        public const string SCHEDULING_DEFAULT_WORK_ON_SUNDAY = "SCHEDULING_DEFAULT_WORK_ON_SUNDAY";
        public const string SCHEDULING_DEFAULT_PERFORMS_SHIFT_WORK = "SCHEDULING_DEFAULT_PERFORMS_SHIFT_WORK";

        public const string SCHEDULE_COMMAND_KEYWORD_FREE = "SCHEDULE_COMMAND_KEYWORD_FREE";
        public const string SCHEDULE_COMMAND_KEYWORD_EARLY = "SCHEDULE_COMMAND_KEYWORD_EARLY";
        public const string SCHEDULE_COMMAND_KEYWORD_LATE = "SCHEDULE_COMMAND_KEYWORD_LATE";
        public const string SCHEDULE_COMMAND_KEYWORD_NIGHT = "SCHEDULE_COMMAND_KEYWORD_NIGHT";
        public const string SCHEDULE_COMMAND_KEYWORD_NEG_FREE = "SCHEDULE_COMMAND_KEYWORD_NEG_FREE";
        public const string SCHEDULE_COMMAND_KEYWORD_NEG_EARLY = "SCHEDULE_COMMAND_KEYWORD_NEG_EARLY";
        public const string SCHEDULE_COMMAND_KEYWORD_NEG_LATE = "SCHEDULE_COMMAND_KEYWORD_NEG_LATE";
        public const string SCHEDULE_COMMAND_KEYWORD_NEG_NIGHT = "SCHEDULE_COMMAND_KEYWORD_NEG_NIGHT";

        public const string DATA_RETENTION_DAYS = "DATA_RETENTION_DAYS";
        public const int DATA_RETENTION_DAYS_DEFAULT = 3650;

        public const string ASSISTANT_STT_PROVIDER = "ASSISTANT_STT_PROVIDER";
        public const string ASSISTANT_STT_API_KEY = "ASSISTANT_STT_API_KEY";
        public const string ASSISTANT_TRANSCRIPTION_MODEL = "ASSISTANT_TRANSCRIPTION_MODEL";
        public const string ASSISTANT_TRANSCRIPTION_PROMPT = "ASSISTANT_TRANSCRIPTION_PROMPT";
        public const string ASSISTANT_ENHANCEMENT_ENABLED = "ASSISTANT_ENHANCEMENT_ENABLED";
        public const string ASSISTANT_OUTPUT_MODE = "ASSISTANT_OUTPUT_MODE";
        public const string ASSISTANT_TTS_PROVIDER = "ASSISTANT_TTS_PROVIDER";
        public const string ASSISTANT_TTS_VOICE = "ASSISTANT_TTS_VOICE";
        public const string ASSISTANT_SILENCE_THRESHOLD_MS = "ASSISTANT_SILENCE_THRESHOLD_MS";

        public const string WIZARD3_LLM_MODEL = "WIZARD3_LLM_MODEL";
    }
}

